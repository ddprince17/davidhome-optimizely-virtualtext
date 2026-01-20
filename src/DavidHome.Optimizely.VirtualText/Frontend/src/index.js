import './app.css';

  var monaco = window.VirtualTextMonaco && window.VirtualTextMonaco.monaco;

function resolveContainer(containerOrId) {
  if (typeof containerOrId === 'string') {
    return document.getElementById(containerOrId);
  }

  return containerOrId;
}

function createEditor(containerOrId, value, options) {
  var container = resolveContainer(containerOrId);
  if (!container) {
    throw new Error('Monaco container not found.');
  }

  if (!monaco) {
    throw new Error('Monaco not loaded. Load monaco-editor.js before virtualtext-app.js.');
  }

  return monaco.editor.create(
    container,
    Object.assign(
      {
        value: value || '',
        language: 'plaintext',
        automaticLayout: true
      },
      options || {}
    )
  );
}

window.VirtualTextMonaco = {
  createEditor: createEditor,
  monaco: monaco
};

(function () {
  var wrapper = document.querySelector('.vt-wrapper');
  if (!wrapper) {
    return;
  }

  var endpoints = {
    FileContentUrl: wrapper.getAttribute('data-file-content-url'),
    SaveFileUrl: wrapper.getAttribute('data-save-file-url'),
    CopyFileUrl: wrapper.getAttribute('data-copy-file-url')
  };

  var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
  var antiForgeryToken = tokenInput ? tokenInput.value : '';
  var editor;
  var editorModal = document.getElementById('vt-editor-modal');
  var editorTitle = document.getElementById('vt-editor-title');
  var editorSubtitle = document.getElementById('vt-editor-subtitle');
  var editorContainer = document.getElementById('vt-editor');
  var permissionWarning = document.getElementById('vt-permission-warning');
  var permissionClose = permissionWarning ? permissionWarning.querySelector('.vt-alert-close') : null;
  var compareSite = document.getElementById('vt-compare-site');
  var compareStart = document.getElementById('vt-compare-start');
  var compareAccept = document.getElementById('vt-compare-accept');
  var compareCancel = document.getElementById('vt-compare-cancel');
  var isDirty = false;
  var isReadOnly = false;
  var currentFile = null;
  var diffEditor = null;
  var diffOriginal = null;
  var diffModified = null;
  var editorContentBeforeDiff = null;
  var dirtyBeforeDiff = false;
  var permissionTimer = null;

  function setDirty(dirty) {
    isDirty = dirty;
    editorTitle.textContent = currentFile
      ? (dirty ? currentFile.virtualPath + ' *' : currentFile.virtualPath)
      : 'Editor';
  }

  function openModal() {
    editorModal.setAttribute('aria-hidden', 'false');
  }

  function closeModal() {
    editorModal.setAttribute('aria-hidden', 'true');
    disposeEditors();
    isDirty = false;
    isReadOnly = false;
    currentFile = null;
    editorContainer.innerHTML = '';
    document.getElementById('vt-save').disabled = false;
    if (compareStart) {
      compareStart.disabled = false;
    }
    if (permissionWarning) {
      permissionWarning.hidden = true;
    }
    if (permissionTimer) {
      window.clearTimeout(permissionTimer);
      permissionTimer = null;
    }
    setCompareMode(false);
  }

  function confirmDiscard() {
    if (!isDirty) {
      return true;
    }

    return window.confirm('You have unsaved changes. Close without saving?');
  }

  function attemptClose() {
    if (confirmDiscard()) {
      closeModal();
    }
  }

  function setHeader(file) {
    editorTitle.textContent = file.virtualPath;
    editorSubtitle.textContent = file.siteName || 'Default (All Sites)';
  }

  function loadContent(file, readOnly) {
    var url = endpoints.FileContentUrl + '?virtualPath=' + encodeURIComponent(file.virtualPath);
    if (file.siteId) {
      url += '&siteId=' + encodeURIComponent(file.siteId);
    }
    return fetch(url).then(function (response) {
      if (!response.ok) {
        throw new Error('Failed to load file.');
      }
      return response.text();
    }).then(function (content) {
      openEditor(file, content, readOnly);
    });
  }

  function openEditor(file, content, readOnly) {
    currentFile = file;
    isReadOnly = !!readOnly;
    setHeader(file);
    editorContainer.innerHTML = '';
    editor = window.VirtualTextMonaco.createEditor(editorContainer, content || '', {
      language: 'plaintext',
      readOnly: isReadOnly
    });
    document.getElementById('vt-save').disabled = isReadOnly;
    if (compareStart) {
      compareStart.disabled = isReadOnly;
    }
    if (permissionWarning) {
      permissionWarning.hidden = true;
    }
    if (permissionTimer) {
      window.clearTimeout(permissionTimer);
      permissionTimer = null;
    }
    setCompareMode(false);
    setDirty(false);
    if (!isReadOnly) {
      editor.onDidChangeModelContent(function () {
        setDirty(true);
      });
    }
    openModal();
  }

  function saveCurrent() {
    if (!currentFile || isReadOnly) {
      return;
    }
    var payload = {
      virtualPath: currentFile.virtualPath,
      siteId: currentFile.siteId,
      content: editor.getValue()
    };
    return fetch(endpoints.SaveFileUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': antiForgeryToken
      },
      body: JSON.stringify(payload)
    }).then(function (response) {
      if (isPermissionDenied(response)) {
        showPermissionWarning('You do not have permission to save files.');
        throw new Error('Permission denied.');
      }
      if (!response.ok) {
        throw new Error('Failed to save.');
      }
      setDirty(false);
    });
  }

  function setCompareMode(enabled) {
    if (!compareAccept || !compareCancel || !compareStart || !compareSite) {
      return;
    }
    compareAccept.hidden = !enabled;
    compareCancel.hidden = !enabled;
    compareStart.hidden = enabled;
    compareSite.disabled = enabled || isReadOnly;
  }

  function disposeEditors() {
    if (diffEditor) {
      diffEditor.dispose();
      diffEditor = null;
    }
    if (diffOriginal) {
      diffOriginal.dispose();
      diffOriginal = null;
    }
    if (diffModified) {
      diffModified.dispose();
      diffModified = null;
    }
    if (editor) {
      editor.dispose();
      editor = null;
    }
  }

  function enterDiffMode(sourceContent) {
    editorContentBeforeDiff = editor.getValue();
    dirtyBeforeDiff = isDirty;
    disposeEditors();
    diffOriginal = monaco.editor.createModel(editorContentBeforeDiff || '', 'plaintext');
    diffModified = monaco.editor.createModel(sourceContent || '', 'plaintext');
    diffEditor = monaco.editor.createDiffEditor(editorContainer, {
      readOnly: true,
      automaticLayout: true
    });
    diffEditor.setModel({
      original: diffOriginal,
      modified: diffModified
    });
    setCompareMode(true);
  }

  function exitDiffMode(applyChanges) {
    var contentToApply = applyChanges ? diffModified.getValue() : editorContentBeforeDiff;
    disposeEditors();
    editorContainer.innerHTML = '';
    editor = window.VirtualTextMonaco.createEditor(editorContainer, contentToApply || '', {
      language: 'plaintext',
      readOnly: isReadOnly
    });
    setDirty(applyChanges || dirtyBeforeDiff);
    if (!isReadOnly) {
      editor.onDidChangeModelContent(function () {
        setDirty(true);
      });
    }
    editorContentBeforeDiff = null;
    dirtyBeforeDiff = false;
    setCompareMode(false);
  }

  function copyFromDefault(targetFile) {
    var payload = {
      virtualPath: targetFile.virtualPath,
      sourceSiteId: null,
      targetSiteId: targetFile.siteId
    };
    return fetch(endpoints.CopyFileUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': antiForgeryToken
      },
      body: JSON.stringify(payload)
    }).then(function (response) {
      if (isPermissionDenied(response)) {
        showPermissionWarning('You do not have permission to copy files.');
        throw new Error('Permission denied.');
      }
      if (!response.ok) {
        throw new Error('Failed to copy.');
      }
    });
  }

  function ensureRow(file) {
    var selector = 'tr[data-virtual-path="' + file.virtualPath.replace(/"/g, '\\"') + '"][data-site-id="' + (file.siteId || '') + '"]';
    var existing = document.querySelector(selector);
    if (existing) {
      return;
    }
    var row = document.createElement('tr');
    row.setAttribute('data-virtual-path', file.virtualPath);
    row.setAttribute('data-site-id', file.siteId || '');
    row.innerHTML = '<td>' + file.virtualPath + '</td>' +
      '<td>' + (file.siteName || 'Default (All Sites)') + '</td>' +
      '<td>' +
      '<button type="button" class="vt-action" data-action="view">View</button>' +
      '<button type="button" class="vt-action" data-action="edit">Edit</button>' +
      '</td>';
    document.getElementById('vt-file-list').appendChild(row);
  }

  document.getElementById('vt-file-list').addEventListener('click', function (event) {
    var target = event.target;
    if (!target.classList.contains('vt-action')) {
      return;
    }
    var row = target.closest('tr');
    var file = {
      virtualPath: row.getAttribute('data-virtual-path'),
      siteId: row.getAttribute('data-site-id') || null,
      siteName: row.children[1].textContent
    };
    var action = target.getAttribute('data-action');
    if (action === 'view') {
      loadContent(file, true);
    } else if (action === 'edit') {
      loadContent(file, false);
    }
  });

  document.getElementById('vt-create-file').addEventListener('click', function () {
    var pathInput = document.getElementById('vt-new-path');
    var siteSelect = document.getElementById('vt-new-site');
    var copyDefault = document.getElementById('vt-copy-default').checked;
    var virtualPath = pathInput.value.trim();
    if (!virtualPath) {
      window.alert('Please enter a virtual path.');
      return;
    }
    var siteId = siteSelect.value || null;
    var siteName = siteSelect.options[siteSelect.selectedIndex].text;
    var newFile = {
      virtualPath: virtualPath,
      siteId: siteId,
      siteName: siteName
    };

    var createPromise = copyDefault && siteId
      ? copyFromDefault(newFile)
      : saveCurrentNew(newFile);

    createPromise.then(function () {
      ensureRow(newFile);
      loadContent(newFile, false);
      pathInput.value = '';
      document.getElementById('vt-copy-default').checked = false;
    }).catch(function (error) {
      window.alert(error.message || 'Failed to create file.');
    });
  });

  function saveCurrentNew(file) {
    var payload = {
      virtualPath: file.virtualPath,
      siteId: file.siteId,
      content: ''
    };
    return fetch(endpoints.SaveFileUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': antiForgeryToken
      },
      body: JSON.stringify(payload)
    }).then(function (response) {
      if (isPermissionDenied(response)) {
        showPermissionWarning('You do not have permission to create files.');
        throw new Error('Permission denied.');
      }
      if (!response.ok) {
        throw new Error('Failed to create.');
      }
    });
  }

  document.getElementById('vt-save').addEventListener('click', function () {
    saveCurrent();
  });

  document.getElementById('vt-close').addEventListener('click', function () {
    attemptClose();
  });

  editorModal.addEventListener('click', function (event) {
    if (event.target.classList.contains('vt-modal-backdrop')) {
      attemptClose();
    }
  });

  window.addEventListener('keydown', function (event) {
    if (!editor || editorModal.getAttribute('aria-hidden') === 'true') {
      return;
    }
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') {
      event.preventDefault();
      saveCurrent();
    }
    if (event.key === 'Escape') {
      event.preventDefault();
      attemptClose();
    }
  });

  function isPermissionDenied(response) {
    return response.status === 401 || response.status === 403 || response.redirected;
  }

  function showPermissionWarning(message) {
    if (!permissionWarning) {
      return;
    }
    var messageNode = permissionWarning.querySelector('span');
    if (messageNode) {
      messageNode.textContent = message;
    }
    permissionWarning.hidden = false;
    if (permissionTimer) {
      window.clearTimeout(permissionTimer);
    }
    permissionTimer = window.setTimeout(function () {
      permissionWarning.hidden = true;
      permissionTimer = null;
    }, 10000);
  }

  function handleCompareStart() {
    if (!currentFile || !editor || !compareSite) {
      return;
    }
    if (isReadOnly) {
      return;
    }
    var sourceSiteId = compareSite.value || null;
    if (sourceSiteId === (currentFile.siteId || '')) {
      window.alert('Select a different site to compare.');
      return;
    }
    var url = endpoints.FileContentUrl + '?virtualPath=' + encodeURIComponent(currentFile.virtualPath);
    if (sourceSiteId) {
      url += '&siteId=' + encodeURIComponent(sourceSiteId);
    }
    fetch(url).then(function (response) {
      if (isPermissionDenied(response)) {
        showPermissionWarning('You do not have permission to read the source file.');
        throw new Error('Permission denied.');
      }
      if (!response.ok) {
        throw new Error('Failed to load source file.');
      }
      return response.text();
    }).then(function (content) {
      enterDiffMode(content);
    }).catch(function (error) {
      window.alert(error.message || 'Failed to load source file.');
    });
  }

  function handleCompareAccept() {
    if (!diffEditor || !diffModified) {
      return;
    }
    exitDiffMode(true);
    var savePromise = saveCurrent();
    if (savePromise && savePromise.catch) {
      savePromise.catch(function (error) {
        window.alert(error.message || 'Failed to save.');
      });
    }
  }

  function handleCompareCancel() {
    if (!diffEditor) {
      return;
    }
    exitDiffMode(false);
  }

  if (permissionClose) {
    permissionClose.addEventListener('click', function () {
      if (permissionWarning) {
        permissionWarning.hidden = true;
      }
      if (permissionTimer) {
        window.clearTimeout(permissionTimer);
        permissionTimer = null;
      }
    });
  }

  if (compareStart) {
    compareStart.addEventListener('click', handleCompareStart);
  }
  if (compareAccept) {
    compareAccept.addEventListener('click', handleCompareAccept);
  }
  if (compareCancel) {
    compareCancel.addEventListener('click', handleCompareCancel);
  }
})();
