import './app.css';

type MonacoApi = typeof import('monaco-editor');
type MonacoEditor = import('monaco-editor').editor.IStandaloneCodeEditor;
type MonacoDiffEditor = import('monaco-editor').editor.IStandaloneDiffEditor;
type MonacoModel = import('monaco-editor').editor.ITextModel;
type MonacoEditorOptions = import('monaco-editor').editor.IStandaloneEditorConstructionOptions;

declare global {
  interface Window {
    VirtualTextMonaco?: {
      createEditor: (containerOrId: HTMLElement | string, value?: string, options?: MonacoEditorOptions) => Promise<MonacoEditor>;
      monaco?: MonacoApi | null;
    };
  }
}

let monaco: MonacoApi | null = null;
let monacoLoading: Promise<MonacoApi> | null = null;
let monacoLoadingStart: (() => void) | null = null;
let monacoLoadingEnd: (() => void) | null = null;

function loadMonaco(): Promise<MonacoApi> {
  if (monaco) {
    return Promise.resolve(monaco);
  }
  if (!monacoLoading) {
    if (monacoLoadingStart) {
      monacoLoadingStart();
    }
    monacoLoading = Promise.all([
      import('monaco-editor')
    ]).then(function (modules) {
      monaco = modules[0] as MonacoApi;
      const vtMonaco = getVirtualTextMonaco();
      vtMonaco.monaco = monaco;
      return monaco;
    }).then(function (loadedMonaco) {
      if (monacoLoadingEnd) {
        monacoLoadingEnd();
      }
      return loadedMonaco;
    }, function (error) {
      if (monacoLoadingEnd) {
        monacoLoadingEnd();
      }
      throw error;
    });
  }
  return monacoLoading;
}

function getRequiredElement<T extends HTMLElement>(id: string): T {
  const element = document.getElementById(id);
  if (!element) {
    throw new Error('Required element not found: ' + id);
  }
  return element as T;
}

function getRequiredAttribute(element: Element, name: string): string {
  const value = element.getAttribute(name);
  if (value === null) {
    throw new Error('Required attribute missing: ' + name);
  }
  return value;
}

function getMonacoApi() {
  if (!window.VirtualTextMonaco) {
    throw new Error('Monaco API is not available.');
  }
  return window.VirtualTextMonaco;
}

function getVirtualTextMonaco() {
  if (!window.VirtualTextMonaco) {
    window.VirtualTextMonaco = {
      createEditor: createEditor,
      monaco: null
    };
  }
  return window.VirtualTextMonaco;
}

function resolveContainer(containerOrId: HTMLElement | string): HTMLElement | null {
  if (typeof containerOrId === 'string') {
    return document.getElementById(containerOrId);
  }

  return containerOrId;
}

async function createEditor(containerOrId: HTMLElement | string, value?: string, options?: MonacoEditorOptions): Promise<MonacoEditor> {
  const container = resolveContainer(containerOrId);
  if (!container) {
    throw new Error('Monaco container not found.');
  }

  const loadedMonaco = await loadMonaco();
  return loadedMonaco.editor.create(
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

window.VirtualTextMonaco = getVirtualTextMonaco();

(function () {
  const wrapper = document.querySelector('.vt-wrapper');
  if (!wrapper) {
    return;
  }

  const endpoints = {
    FileContentUrl: getRequiredAttribute(wrapper, 'data-file-content-url'),
    SaveFileUrl: getRequiredAttribute(wrapper, 'data-save-file-url'),
    CopyFileUrl: getRequiredAttribute(wrapper, 'data-copy-file-url')
  };

  const tokenInput = document.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]');
  const antiForgeryToken = tokenInput ? tokenInput.value : '';
  let editor: MonacoEditor | null = null;
  const editorModal = getRequiredElement<HTMLDivElement>('vt-editor-modal');
  const editorTitle = getRequiredElement<HTMLDivElement>('vt-editor-title');
  const editorSubtitle = getRequiredElement<HTMLDivElement>('vt-editor-subtitle');
  const editorContainer = getRequiredElement<HTMLDivElement>('vt-editor');
  const editorLoading = document.getElementById('vt-editor-loading');
  const permissionWarning = document.getElementById('vt-permission-warning');
  const permissionClose = permissionWarning ? permissionWarning.querySelector('.vt-alert-close') : null;
  const compareSite = document.getElementById('vt-compare-site') as HTMLSelectElement | null;
  const compareStart = document.getElementById('vt-compare-start') as HTMLButtonElement | null;
  const compareAccept = document.getElementById('vt-compare-accept') as HTMLButtonElement | null;
  const compareCancel = document.getElementById('vt-compare-cancel') as HTMLButtonElement | null;
  let isDirty = false;
  let isReadOnly = false;
  let currentFile: { virtualPath: string; siteId: string | null; siteName: string } | null = null;
  let diffEditor: MonacoDiffEditor | null = null;
  let diffOriginal: MonacoModel | null = null;
  let diffModified: MonacoModel | null = null;
  let editorContentBeforeDiff: string | null = null;
  let dirtyBeforeDiff = false;
  let permissionTimer: number | null = null;

  function setDirty(dirty: boolean) {
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
    setEditorLoading(false);
    getRequiredElement<HTMLButtonElement>('vt-save').disabled = false;
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

  function setHeader(file: { virtualPath: string; siteId: string | null; siteName: string }) {
    editorTitle.textContent = file.virtualPath;
    editorSubtitle.textContent = file.siteName || 'Default (All Sites)';
  }

  function setEditorLoading(loading: boolean) {
    if (!editorLoading) {
      return;
    }
    editorLoading.hidden = !loading;
  }

  monacoLoadingStart = function () {
    setEditorLoading(true);
  };

  monacoLoadingEnd = function () {
    setEditorLoading(false);
  };

  async function loadContent(file: { virtualPath: string; siteId: string | null; siteName: string }, readOnly: boolean) {
    let url = endpoints.FileContentUrl + '?virtualPath=' + encodeURIComponent(file.virtualPath);
    if (file.siteId) {
      url += '&siteId=' + encodeURIComponent(file.siteId);
    }
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error('Failed to load file.');
    }
    const content = await response.text();
    return await openEditor(file, content, readOnly);
  }

  async function openEditor(file: { virtualPath: string; siteId: string | null; siteName: string }, content: string, readOnly: boolean) {
    currentFile = file;
    isReadOnly = readOnly;
    setHeader(file);
    editorContainer.innerHTML = '';
    const monacoApi = getMonacoApi();
    try {
      editor = await monacoApi.createEditor(editorContainer, content || '', {
        language: 'plaintext',
        readOnly: isReadOnly
      });
      getRequiredElement<HTMLButtonElement>('vt-save').disabled = isReadOnly;
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
    } catch (error: any) {
      window.alert(error && error.message ? error.message : 'Failed to load editor.');
    }
  }

  function saveCurrent() {
    if (!currentFile || isReadOnly || !editor) {
      return;
    }
    const payload = {
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

  function setCompareMode(enabled: boolean) {
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

  function enterDiffMode(sourceContent: string) {
    if (!editor || !monaco) {
      return;
    }
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

  async function exitDiffMode(applyChanges: boolean) {
    const contentToApply = applyChanges && diffModified ? diffModified.getValue() : editorContentBeforeDiff;
    disposeEditors();
    editorContainer.innerHTML = '';
    const monacoApi = getMonacoApi();
    try {
      editor = await monacoApi.createEditor(editorContainer, contentToApply || '', {
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
    } catch (error: any) {
      window.alert(error && error.message ? error.message : 'Failed to load editor.');
    }
  }

  async function copyFromDefault(targetFile: { virtualPath: string; siteId: string | null; siteName: string }) {
    const payload = {
      virtualPath: targetFile.virtualPath,
      sourceSiteId: null,
      targetSiteId: targetFile.siteId
    };
    const response = await fetch(endpoints.CopyFileUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': antiForgeryToken
      },
      body: JSON.stringify(payload)
    });
    if (isPermissionDenied(response)) {
      showPermissionWarning('You do not have permission to copy files.');
      throw new Error('Permission denied.');
    }
    if (!response.ok) {
      throw new Error('Failed to copy.');
    }
  }

  function ensureRow(file: { virtualPath: string; siteId: string | null; siteName: string }) {
    const selector = 'tr[data-virtual-path="' + file.virtualPath.replace(/"/g, '\\"') + '"][data-site-id="' + (file.siteId || '') + '"]';
    const existing = document.querySelector(selector);
    if (existing) {
      return;
    }
    const row = document.createElement('tr');
    row.setAttribute('data-virtual-path', file.virtualPath);
    row.setAttribute('data-site-id', file.siteId || '');
    row.innerHTML = '<td>' + file.virtualPath + '</td>' +
      '<td>' + (file.siteName || 'Default (All Sites)') + '</td>' +
      '<td>' +
      '<button type="button" class="vt-action" data-action="view">View</button>' +
      '<button type="button" class="vt-action" data-action="edit">Edit</button>' +
      '</td>';
    getRequiredElement<HTMLTableElement>('vt-file-list').appendChild(row);
  }

  getRequiredElement<HTMLTableElement>('vt-file-list').addEventListener('click', async function (event) {
    if (!(event.target instanceof HTMLElement)) {
      return;
    }
    const target = event.target;
    if (!target.classList.contains('vt-action')) {
      return;
    }
    const row = target.closest('tr');
    if (!row) {
      return;
    }
    const file = {
      virtualPath: getRequiredAttribute(row, 'data-virtual-path'),
      siteId: row.getAttribute('data-site-id') || null,
      siteName: row.children[1].textContent || ''
    };
    const action = target.getAttribute('data-action');
    if (action === 'view') {
      await loadContent(file, true);
    } else if (action === 'edit') {
      await loadContent(file, false);
    }
  });

  getRequiredElement<HTMLButtonElement>('vt-create-file').addEventListener('click', function () {
    const pathInput = getRequiredElement<HTMLInputElement>('vt-new-path');
    const siteSelect = getRequiredElement<HTMLSelectElement>('vt-new-site');
    const copyDefault = getRequiredElement<HTMLInputElement>('vt-copy-default').checked;
    const virtualPath = pathInput.value.trim();
    if (!virtualPath) {
      window.alert('Please enter a virtual path.');
      return;
    }
    const siteId = siteSelect.value || null;
    const siteName = siteSelect.options[siteSelect.selectedIndex].text;
    const newFile = {
      virtualPath: virtualPath,
      siteId: siteId,
      siteName: siteName
    };

    const createPromise = copyDefault && siteId
      ? copyFromDefault(newFile)
      : saveCurrentNew(newFile);

    createPromise.then(async function () {
      ensureRow(newFile);
      await loadContent(newFile, false);
      pathInput.value = '';
      getRequiredElement<HTMLInputElement>('vt-copy-default').checked = false;
    }).catch(function (error) {
      window.alert(error.message || 'Failed to create file.');
    });
  });

  async function saveCurrentNew(file: { virtualPath: string; siteId: string | null; siteName: string }) {
    const payload = {
      virtualPath: file.virtualPath,
      siteId: file.siteId,
      content: ''
    };
    const response = await fetch(endpoints.SaveFileUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': antiForgeryToken
      },
      body: JSON.stringify(payload)
    });
    if (isPermissionDenied(response)) {
      showPermissionWarning('You do not have permission to create files.');
      throw new Error('Permission denied.');
    }
    if (!response.ok) {
      throw new Error('Failed to create.');
    }
  }

  getRequiredElement<HTMLButtonElement>('vt-save').addEventListener('click', function () {
    saveCurrent();
  });

  getRequiredElement<HTMLButtonElement>('vt-close').addEventListener('click', function () {
    attemptClose();
  });

  editorModal.addEventListener('click', function (event) {
    if (event.target instanceof HTMLElement && event.target.classList.contains('vt-modal-backdrop')) {
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

  function isPermissionDenied(response: Response) {
    return response.status === 401 || response.status === 403 || response.redirected;
  }

  function showPermissionWarning(message: string) {
    if (!permissionWarning) {
      return;
    }
    const messageNode = permissionWarning.querySelector('span');
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
    const sourceSiteId = compareSite.value || null;
    if (sourceSiteId === (currentFile.siteId || '')) {
      window.alert('Select a different site to compare.');
      return;
    }
    let url = endpoints.FileContentUrl + '?virtualPath=' + encodeURIComponent(currentFile.virtualPath);
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
    exitDiffMode(true).then(function () {
      const savePromise = saveCurrent();
      if (savePromise && savePromise.catch) {
        savePromise.catch(function (error) {
          window.alert(error.message || 'Failed to save.');
        });
      }
    });
  }

  async function handleCompareCancel() {
    if (!diffEditor) {
      return;
    }
    await exitDiffMode(false);
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
