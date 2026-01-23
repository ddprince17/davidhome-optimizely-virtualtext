import './app.css';

type MonacoApi = typeof import('monaco-editor');
type MonacoEditor = import('monaco-editor').editor.IStandaloneCodeEditor;
type MonacoDiffEditor = import('monaco-editor').editor.IStandaloneDiffEditor;
type MonacoModel = import('monaco-editor').editor.ITextModel;
type MonacoEditorOptions = import('monaco-editor').editor.IStandaloneEditorConstructionOptions;
type VirtualTextFileListItem = {
  virtualPath: string;
  siteId: string | null;
  siteName: string;
  isDefault: boolean;
};

type VirtualTextFileListResponse = {
  files: VirtualTextFileListItem[];
  hasMore: boolean;
};

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
    CopyFileUrl: getRequiredAttribute(wrapper, 'data-copy-file-url'),
    DeleteFileUrl: getRequiredAttribute(wrapper, 'data-delete-file-url'),
    FileListUrl: getRequiredAttribute(wrapper, 'data-file-list-url')
  };
  const canEdit = getRequiredAttribute(wrapper, 'data-can-edit').toLowerCase() === 'true';

  const tokenInput = document.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]');
  const antiForgeryToken = tokenInput ? tokenInput.value : '';
  let editor: MonacoEditor | null = null;
  const editorModal = getRequiredElement<HTMLDivElement>('vt-editor-modal');
  const editorTitle = getRequiredElement<HTMLDivElement>('vt-editor-title');
  const editorSubtitle = getRequiredElement<HTMLDivElement>('vt-editor-subtitle');
  const editorContainer = getRequiredElement<HTMLDivElement>('vt-editor');
  const editorLoading = document.getElementById('vt-editor-loading');
  const saveIndicator = document.getElementById('vt-save-indicator');
  const permissionWarning = document.getElementById('vt-permission-warning');
  const permissionClose = permissionWarning ? permissionWarning.querySelector('.vt-alert-close') : null;
  const notificationContainer = document.getElementById('vt-notification-container');
  const confirmModal = document.getElementById('vt-confirm-modal');
  const confirmMessage = document.getElementById('vt-confirm-message');
  const confirmAccept = document.getElementById('vt-confirm-accept') as HTMLButtonElement | null;
  const confirmCancel = document.getElementById('vt-confirm-cancel') as HTMLButtonElement | null;
  const fileListBody = getRequiredElement<HTMLTableSectionElement>('vt-file-list');
  const filterPathInput = document.getElementById('vt-filter-path') as HTMLInputElement | null;
  const filterSiteSelect = document.getElementById('vt-filter-site') as HTMLSelectElement | null;
  const loadMoreButton = document.getElementById('vt-load-more') as HTMLButtonElement | null;
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
  let compareTargetSiteId: string | null = null;
  let compareTargetSiteIdSet = false;
  let permissionTimer: number | null = null;
  let listPageNumber = 1;
  let listLoading = false;
  let listHasMore = true;
  let listRefreshTimer: number | null = null;
  let saveIndicatorTimer: number | null = null;
  let confirmResolver: ((value: boolean) => void) | null = null;

  function setDirty(dirty: boolean) {
    isDirty = dirty;
    editorTitle.textContent = currentFile
      ? (dirty ? currentFile.virtualPath + ' *' : currentFile.virtualPath)
      : 'Editor';
  }

  function openModal() {
    editorModal.setAttribute('aria-hidden', 'false');
    editorModal.classList.remove('hidden');
    editorModal.hidden = false;
  }

  function closeModal() {
    editorModal.setAttribute('aria-hidden', 'true');
    editorModal.classList.add('hidden');
    editorModal.hidden = true;
    disposeEditors();
    isDirty = false;
    isReadOnly = false;
    currentFile = null;
    editorContainer.innerHTML = '';
    setEditorLoading(false);
    compareTargetSiteId = null;
    compareTargetSiteIdSet = false;
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
      return Promise.resolve(true);
    }

    return openConfirmModal('You have unsaved changes. Close without saving?', 'Discard changes', 'Keep editing');
  }

  async function attemptClose() {
    const shouldClose = await confirmDiscard();
    if (shouldClose) {
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

  function showSaveIndicator() {
    if (!saveIndicator) {
      return;
    }
    saveIndicator.hidden = false;
    if (saveIndicatorTimer) {
      window.clearTimeout(saveIndicatorTimer);
    }
    saveIndicatorTimer = window.setTimeout(function () {
      saveIndicator.hidden = true;
      saveIndicatorTimer = null;
    }, 2500);
  }

  function showNotification(message: string, type: 'success' | 'error' | 'info' = 'info') {
    if (!notificationContainer) {
      return;
    }
    const notificationDiv = document.createElement('div');
    const typeClass = type === 'success'
      ? 'border-emerald-200 bg-emerald-50 text-emerald-900'
      : type === 'error'
        ? 'border-rose-200 bg-rose-50 text-rose-900'
        : 'border-slate-200 bg-slate-50 text-slate-900';
    notificationDiv.className = 'flex items-start gap-3 rounded-lg border px-4 py-3 text-sm shadow ' + typeClass;
    notificationDiv.innerHTML = '<div class="mt-0.5 h-2 w-2 shrink-0 rounded-full bg-current opacity-60"></div>' +
      '<div class="flex-1">' + message + '</div>';
    notificationContainer.appendChild(notificationDiv);
    window.setTimeout(function () {
      notificationDiv.remove();
    }, 4000);
  }

  function openConfirmModal(message: string, confirmText: string, cancelText: string) {
    if (!confirmModal || !confirmMessage || !confirmAccept || !confirmCancel) {
      return Promise.resolve(false);
    }
    confirmMessage.textContent = message;
    confirmAccept.textContent = confirmText;
    confirmCancel.textContent = cancelText;
    confirmModal.classList.remove('hidden');
    confirmModal.hidden = false;
    confirmModal.setAttribute('aria-hidden', 'false');
    return new Promise<boolean>(function (resolve) {
      confirmResolver = resolve;
    });
  }

  function closeConfirmModal(result: boolean) {
    if (!confirmModal) {
      return;
    }
    confirmModal.classList.add('hidden');
    confirmModal.hidden = true;
    confirmModal.setAttribute('aria-hidden', 'true');
    if (confirmResolver) {
      confirmResolver(result);
      confirmResolver = null;
    }
  }

  monacoLoadingStart = function () {
    setEditorLoading(true);
  };

  monacoLoadingEnd = function () {
    setEditorLoading(false);
  };

  function getSiteNameById(siteId: string | null) {
    if (!compareSite) {
      return siteId ? siteId : 'Default (All Sites)';
    }
    for (let i = 0; i < compareSite.options.length; i += 1) {
      const option = compareSite.options[i];
      if ((option.value || null) === siteId) {
        return option.text;
      }
    }
    return siteId ? siteId : 'Default (All Sites)';
  }

  function getFileListQuery() {
    return {
      path: filterPathInput ? filterPathInput.value.trim() : '',
      siteId: filterSiteSelect ? filterSiteSelect.value || null : null
    };
  }

  function buildFileListUrl(pageNumber: number) {
    const url = new URL(endpoints.FileListUrl, window.location.href);
    const query = getFileListQuery();
    url.searchParams.set('pageNumber', String(pageNumber));
    if (query.path) {
      url.searchParams.set('virtualPath', query.path);
    }
    if (query.siteId) {
      url.searchParams.set('siteId', query.siteId);
    }
    return url.toString();
  }

  function setLoadMoreState() {
    if (!loadMoreButton) {
      return;
    }
    loadMoreButton.disabled = listLoading || !listHasMore;
    loadMoreButton.hidden = !listHasMore;
  }

  function createFileRow(file: VirtualTextFileListItem) {
    const row = document.createElement('tr');
    row.setAttribute('data-virtual-path', file.virtualPath);
    row.setAttribute('data-site-id', file.siteId || '');
    row.innerHTML = '<td class="px-3 py-2">' + file.virtualPath + '</td>' +
      '<td class="px-3 py-2">' + file.siteName + '</td>' +
      '<td class="px-3 py-2">' +
      '<div class="flex items-center gap-2">' +
      '<button type="button" class="vt-action rounded-md border border-slate-300 px-2 py-1 text-xs font-medium text-slate-700 hover:bg-slate-50" data-action="view">View</button>' +
      (canEdit
        ? '<button type="button" class="vt-action rounded-md border border-slate-300 px-2 py-1 text-xs font-medium text-slate-700 hover:bg-slate-50" data-action="edit">Edit</button>' +
          '<button type="button" class="vt-action rounded-md border border-rose-200 px-2 py-1 text-xs font-medium text-rose-700 hover:bg-rose-50" data-action="delete">Delete</button>'
        : '') +
      '</div>' +
      '</td>';
    return row;
  }

  function clearFileList() {
    fileListBody.innerHTML = '';
  }

  async function loadFileListPage(pageNumber: number, append: boolean) {
    if (listLoading) {
      return;
    }
    listLoading = true;
    setLoadMoreState();
    try {
      const response = await fetch(buildFileListUrl(pageNumber));
      if (!response.ok) {
        throw new Error('Failed to load file list.');
      }
      const data = await response.json() as VirtualTextFileListResponse;
      if (!append) {
        clearFileList();
      }
      data.files.forEach(function (file) {
        fileListBody.appendChild(createFileRow(file));
      });
      listHasMore = data.hasMore && data.files.length > 0;
      if (data.files.length > 0) {
        listPageNumber = pageNumber;
      }
    } catch (error: any) {
      showNotification(error && error.message ? error.message : 'Failed to load file list.', 'error');
    } finally {
      listLoading = false;
      setLoadMoreState();
    }
  }

  function refreshFileList(reset: boolean) {
    if (reset) {
      listPageNumber = 1;
      listHasMore = true;
      clearFileList();
    }
    return loadFileListPage(reset ? 1 : listPageNumber + 1, !reset);
  }

  function finishCopySuccess() {
    if (!currentFile) {
      closeModal();
      compareTargetSiteId = null;
      compareTargetSiteIdSet = false;
      return;
    }
    refreshFileList(true);
    closeModal();
    compareTargetSiteId = null;
    compareTargetSiteIdSet = false;
  }

  async function loadContent(file: { virtualPath: string; siteId: string | null; siteName: string }, readOnly: boolean) {
    try {
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
    } catch (error: any) {
      showNotification(error && error.message ? error.message : 'Failed to load file.', 'error');
    }
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
      showNotification(error && error.message ? error.message : 'Failed to load editor.', 'error');
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
      showSaveIndicator();
      showNotification('Saved', 'success');
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
    diffOriginal = monaco.editor.createModel(sourceContent || '', 'plaintext');
    diffModified = monaco.editor.createModel(editorContentBeforeDiff || '', 'plaintext');
    diffEditor = monaco.editor.createDiffEditor(editorContainer, {
      readOnly: false,
      originalEditable: false,
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
      compareTargetSiteId = null;
      compareTargetSiteIdSet = false;
      setCompareMode(false);
    } catch (error: any) {
      showNotification(error && error.message ? error.message : 'Failed to load editor.', 'error');
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
    row.innerHTML = '<td class="px-3 py-2">' + file.virtualPath + '</td>' +
      '<td class="px-3 py-2">' + (file.siteName || 'Default (All Sites)') + '</td>' +
      '<td class="px-3 py-2">' +
      '<div class="flex items-center gap-2">' +
      '<button type="button" class="vt-action rounded-md border border-slate-300 px-2 py-1 text-xs font-medium text-slate-700 hover:bg-slate-50" data-action="view">View</button>' +
      (canEdit
        ? '<button type="button" class="vt-action rounded-md border border-slate-300 px-2 py-1 text-xs font-medium text-slate-700 hover:bg-slate-50" data-action="edit">Edit</button>' +
          '<button type="button" class="vt-action rounded-md border border-rose-200 px-2 py-1 text-xs font-medium text-rose-700 hover:bg-rose-50" data-action="delete">Delete</button>'
        : '') +
      '</div>' +
      '</td>';
    fileListBody.appendChild(row);
  }

  fileListBody.addEventListener('click', async function (event) {
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
    } else if (action === 'delete') {
      const confirmDelete = await openConfirmModal(
        'Delete this file? This cannot be undone.',
        'Delete',
        'Cancel'
      );
      if (!confirmDelete) {
        return;
      }
      try {
        await deleteFile(file);
        row.remove();
        if (currentFile &&
          currentFile.virtualPath === file.virtualPath &&
          (currentFile.siteId || '') === (file.siteId || '')) {
          closeModal();
        }
        showNotification('Deleted', 'success');
      } catch (error: any) {
        if (error && error.message === 'Permission denied.') {
          return;
        }
        showNotification(error && error.message ? error.message : 'Failed to delete file.', 'error');
      }
    }
  });

  const createButton = document.getElementById('vt-create-file') as HTMLButtonElement | null;
  if (createButton) {
    createButton.addEventListener('click', async function () {
      const pathInput = getRequiredElement<HTMLInputElement>('vt-new-path');
      const siteSelect = getRequiredElement<HTMLSelectElement>('vt-new-site');
      const virtualPath = pathInput.value.trim();
      if (!virtualPath) {
      showNotification('Please enter a virtual path.', 'error');
      return;
    }
    const siteId = siteSelect.value || null;
    const siteName = siteSelect.options[siteSelect.selectedIndex].text;
    const newFile = {
      virtualPath: virtualPath,
      siteId: siteId,
      siteName: siteName
    };

    try {
      const exists = await checkFileExists(newFile);
      if (exists) {
        const confirmReplace = await openConfirmModal(
          'A file already exists at this path for the selected site. Replace it with a new one?',
          'Replace',
          'Cancel'
        );
        if (!confirmReplace) {
          return;
        }
      }
      await saveCurrentNew(newFile);
      ensureRow(newFile);
      await loadContent(newFile, false);
      pathInput.value = '';
    } catch (error: any) {
      if (error && error.message === 'Permission denied.') {
        return;
      }
      showNotification(error && error.message ? error.message : 'Failed to create file.', 'error');
    }
    });
  }

  async function checkFileExists(file: { virtualPath: string; siteId: string | null }) {
    let url = endpoints.FileContentUrl + '?virtualPath=' + encodeURIComponent(file.virtualPath);
    if (file.siteId) {
      url += '&siteId=' + encodeURIComponent(file.siteId);
    }
    const response = await fetch(url);
    if (isPermissionDenied(response)) {
      showPermissionWarning('You do not have permission to check existing files.');
      throw new Error('Permission denied.');
    }
    if (response.status === 404) {
      return false;
    }
    if (!response.ok) {
      throw new Error('Failed to check existing file.');
    }
    return true;
  }

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

  async function deleteFile(file: { virtualPath: string; siteId: string | null }) {
    const payload = {
      virtualPath: file.virtualPath,
      siteId: file.siteId
    };
    const response = await fetch(endpoints.DeleteFileUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': antiForgeryToken
      },
      body: JSON.stringify(payload)
    });
    if (isPermissionDenied(response)) {
      showPermissionWarning('You do not have permission to delete files.');
      throw new Error('Permission denied.');
    }
    if (response.status === 404) {
      return;
    }
    if (!response.ok) {
      throw new Error('Failed to delete.');
    }
  }

  async function saveToSite(targetSiteId: string | null, content: string) {
    if (!currentFile) {
      return;
    }
    const payload = {
      virtualPath: currentFile.virtualPath,
      siteId: targetSiteId,
      content: content
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
      showPermissionWarning('You do not have permission to save to the target site.');
      throw new Error('Permission denied.');
    }
    if (!response.ok) {
      throw new Error('Failed to save.');
    }
    showSaveIndicator();
    showNotification('Saved', 'success');
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
    if (confirmModal && confirmModal.getAttribute('aria-hidden') === 'false' && event.key === 'Escape') {
      event.preventDefault();
      closeConfirmModal(false);
      return;
    }
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
    showNotification(message, 'error');
    if (permissionWarning) {
      permissionWarning.hidden = true;
    }
    if (permissionTimer) {
      window.clearTimeout(permissionTimer);
      permissionTimer = null;
    }
  }

  async function handleCompareStart() {
    if (!currentFile || !editor || !compareSite) {
      return;
    }
    if (isReadOnly) {
      return;
    }
    const targetSiteId = compareSite.value || null;
    if (targetSiteId === (currentFile.siteId || '')) {
      showNotification('Select a different site to copy to.', 'error');
      return;
    }
    compareTargetSiteId = targetSiteId;
    compareTargetSiteIdSet = true;
    let url = endpoints.FileContentUrl + '?virtualPath=' + encodeURIComponent(currentFile.virtualPath);
    if (targetSiteId) {
      url += '&siteId=' + encodeURIComponent(targetSiteId);
    }
    try {
      const response = await fetch(url);
      if (isPermissionDenied(response)) {
        showPermissionWarning('You do not have permission to read the target file.');
        throw new Error('Permission denied.');
      }
      if (response.status === 404) {
        const confirmCopy = await openConfirmModal('The target site does not have this file yet. Copy it now?', 'Copy now', 'Cancel');
        if (!confirmCopy) {
          compareTargetSiteId = null;
          compareTargetSiteIdSet = false;
          return;
        }
        await saveToSite(compareTargetSiteId, editor.getValue());
        finishCopySuccess();
        return;
      }
      if (!response.ok) {
        throw new Error('Failed to load target file.');
      }
      const content = await response.text();
      enterDiffMode(content);
    } catch (error: any) {
      compareTargetSiteId = null;
      compareTargetSiteIdSet = false;
      showNotification(error && error.message ? error.message : 'Failed to load target file.', 'error');
    }
  }

  function handleCompareAccept() {
    if (!diffEditor || !diffModified || !compareTargetSiteIdSet) {
      return;
    }
    const contentToCopy = diffModified.getValue();
    saveToSite(compareTargetSiteId, contentToCopy).then(function () {
      return exitDiffMode(false).then(function () {
        finishCopySuccess();
      });
    }).catch(function (error) {
      showNotification(error.message || 'Failed to save.', 'error');
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

  if (confirmAccept) {
    confirmAccept.addEventListener('click', function () {
      closeConfirmModal(true);
    });
  }
  if (confirmCancel) {
    confirmCancel.addEventListener('click', function () {
      closeConfirmModal(false);
    });
  }
  if (confirmModal) {
    confirmModal.addEventListener('click', function (event) {
      if (event.target instanceof HTMLElement && event.target.classList.contains('vt-confirm-backdrop')) {
        closeConfirmModal(false);
      }
    });
  }

  if (filterPathInput) {
    filterPathInput.addEventListener('input', function () {
      if (listRefreshTimer) {
        window.clearTimeout(listRefreshTimer);
      }
      listRefreshTimer = window.setTimeout(function () {
        refreshFileList(true);
        listRefreshTimer = null;
      }, 300);
    });
  }

  if (filterSiteSelect) {
    filterSiteSelect.addEventListener('change', function () {
      refreshFileList(true);
    });
  }

  if (loadMoreButton) {
    loadMoreButton.addEventListener('click', function () {
      if (listLoading || !listHasMore) {
        return;
      }
      refreshFileList(false);
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

  refreshFileList(true);
})();
