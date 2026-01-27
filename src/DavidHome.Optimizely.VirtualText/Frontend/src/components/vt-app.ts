import { LitElement, html } from 'lit';
import { customElement, state } from 'lit/decorators.js';
import { createRef, ref } from 'lit/directives/ref.js';
import type { VirtualTextFileListItem, VirtualTextFileListResponse, VirtualTextSiteOption } from './types';
import './vt-create-form';
import './vt-filters';
import './vt-file-list';
import './vt-editor-modal';
import './vt-confirm-modal';
import './vt-toast-container';

type VirtualTextBootstrap = {
  endpoints: {
    fileContentUrl: string;
    saveFileUrl: string;
    copyFileUrl: string;
    deleteFileUrl: string;
    fileListUrl: string;
  };
  sites: VirtualTextSiteOption[];
  files: VirtualTextFileListItem[];
  canEdit: boolean;
  antiForgeryToken: string;
};

type ToastApi = {
  show: (message: string, type?: 'success' | 'error' | 'info') => void;
};

type ConfirmModalApi = {
  open: (message: string, confirmText: string, cancelText: string) => Promise<boolean>;
  close: () => void;
};

type CreateFormApi = {
  reset: () => void;
};

type EditorModalApi = {
  open: (file: { virtualPath: string; siteId: string | null; siteName: string; hostName: string | null }, content: string, readOnly: boolean) => void;
  close: () => void;
  showSaveIndicator: () => void;
  markSaved: () => void;
  showPermissionWarning: (message: string) => void;
  enterDiffMode: (sourceContent: string) => void;
  exitDiffMode: (applyChanges: boolean) => Promise<void>;
};

@customElement('vt-app')
export class VtApp extends LitElement {
  private bootstrap: VirtualTextBootstrap = {
    endpoints: {
      fileContentUrl: '',
      saveFileUrl: '',
      copyFileUrl: '',
      deleteFileUrl: '',
      fileListUrl: ''
    },
    sites: [],
    files: [],
    canEdit: false,
    antiForgeryToken: ''
  };

  private endpoints = this.bootstrap.endpoints;
  private antiForgeryToken = '';
  private listPageNumber = 1;
  private currentFile: { virtualPath: string; siteId: string | null; siteName: string; hostName: string | null } | null = null;
  private compareTargetSiteId: string | null = null;
  private compareTargetHostName: string | null = null;

  private toastRef = createRef<HTMLElement>();
  private confirmModalRef = createRef<HTMLElement>();
  private createFormRef = createRef<HTMLElement>();
  private editorModalRef = createRef<HTMLElement>();

  @state() accessor sites: VirtualTextSiteOption[] = [];
  @state() accessor canEdit = false;
  @state() accessor listFiles: VirtualTextFileListItem[] = [];
  @state() accessor listHasMore = true;
  @state() accessor listLoading = false;
  @state() accessor filterPath = '';
  @state() accessor filterSiteId: string | null = null;
  @state() accessor filterHostName: string | null = null;

  createRenderRoot() {
    return this;
  }

  connectedCallback() {
    super.connectedCallback();
    this.bootstrap = this.parseBootstrap();
    this.endpoints = this.bootstrap.endpoints;
    this.sites = this.bootstrap.sites || [];
    this.canEdit = this.bootstrap.canEdit;
    this.listFiles = this.bootstrap.files || [];
    this.antiForgeryToken = this.bootstrap.antiForgeryToken || '';
  }

  async firstUpdated() {
    this.addEventListener('vt-create', (event) => this.handleCreate(event as CustomEvent));
    this.addEventListener('vt-create-error', (event) => this.handleCreateError(event as CustomEvent));
    this.addEventListener('vt-filter-change', (event) => this.handleFilterChange(event as CustomEvent));
    this.addEventListener('vt-file-action', (event) => this.handleFileAction(event as CustomEvent));
    this.addEventListener('vt-load-more', () => this.handleLoadMore());
    this.addEventListener('vt-save', (event) => this.handleSave(event as CustomEvent));
    this.addEventListener('vt-close-request', (event) => this.handleCloseRequest(event as CustomEvent));
    this.addEventListener('vt-compare-start', (event) => this.handleCompareStart(event as CustomEvent));
    this.addEventListener('vt-compare-accept', (event) => this.handleCompareAccept(event as CustomEvent));
    this.addEventListener('vt-compare-cancel', () => this.handleCompareCancel());
    this.addEventListener('vt-error', (event) => this.handleEditorError(event as CustomEvent));

    await this.refreshFileList(true);
  }

  render() {
    return html`
      <div class="vt-wrapper flex flex-col gap-4 font-sans text-slate-900">
        <div class="vt-header flex flex-col gap-3">
          <vt-create-form .sites=${this.sites} .canEdit=${this.canEdit} ${ref(this.createFormRef)}></vt-create-form>
          <vt-filters .sites=${this.sites}></vt-filters>
        </div>
        <vt-file-list .files=${this.listFiles} .canEdit=${this.canEdit} .hasMore=${this.listHasMore} .loading=${this.listLoading}></vt-file-list>
      </div>
      <vt-editor-modal .sites=${this.sites} ${ref(this.editorModalRef)}></vt-editor-modal>
      <vt-toast-container ${ref(this.toastRef)}></vt-toast-container>
      <vt-confirm-modal ${ref(this.confirmModalRef)}></vt-confirm-modal>
    `;
  }

  private parseBootstrap(): VirtualTextBootstrap {
    const raw = this.getAttribute('data-bootstrap');
    if (raw) {
      try {
        return JSON.parse(raw) as VirtualTextBootstrap;
      } catch {
        return this.bootstrap;
      }
    }

    const bootstrapId = this.getAttribute('data-bootstrap-id');
    if (!bootstrapId) {
      return this.bootstrap;
    }

    const bootstrapNode = document.getElementById(bootstrapId);
    const text = bootstrapNode?.textContent;
    if (!text) {
      return this.bootstrap;
    }

    try {
      return JSON.parse(text) as VirtualTextBootstrap;
    } catch {
      return this.bootstrap;
    }
  }

  private showToast(message: string, type: 'success' | 'error' | 'info' = 'info') {
    const toast = this.getToastApi();
    if (!toast) {
      return;
    }
    toast.show(message, type);
  }

  private handleCreateError(event: CustomEvent) {
    const message = event.detail?.message || 'Please enter a virtual path.';
    this.showToast(message, 'error');
  }

  private async handleCreate(event: CustomEvent) {
    const { virtualPath, siteId, siteName, hostName } = event.detail as {
      virtualPath: string;
      siteId: string | null;
      siteName: string;
      hostName?: string | null;
    };
    const newFile = { virtualPath, siteId, siteName, hostName: hostName ?? null };

    try {
      const exists = await this.checkFileExists(newFile);
      if (exists) {
        const confirmReplace = await this.confirm('A file already exists at this path for the selected site. Replace it with a new one?', 'Replace', 'Cancel');
        if (!confirmReplace) {
          return;
        }
      }
      await this.saveNewFile(newFile);
      await this.refreshFileList(true);
      await this.loadContent(newFile, false);
      this.getCreateFormApi()?.reset();
    } catch (error: any) {
      if (error && error.message === 'Permission denied.') {
        return;
      }
      this.showToast(error && error.message ? error.message : 'Failed to create file.', 'error');
    }
  }

  private async handleFilterChange(event: CustomEvent) {
    const { path, siteId, hostName } = event.detail as { path: string; siteId: string | null; hostName: string | null };
    this.filterPath = path;
    this.filterSiteId = siteId;
    this.filterHostName = hostName;
    await this.refreshFileList(true);
  }

  private async handleFileAction(event: CustomEvent) {
    const { action, file } = event.detail as { action: 'view' | 'edit' | 'delete'; file: VirtualTextFileListItem };
    if (action === 'view') {
      await this.loadContent(file, true);
      return;
    }
    if (action === 'edit') {
      await this.loadContent(file, false);
      return;
    }
    if (action === 'delete') {
      const confirmDelete = await this.confirm('Delete this file? This cannot be undone.', 'Delete', 'Cancel');
      if (!confirmDelete) {
        return;
      }
      try {
        await this.deleteFile(file);
        this.listFiles = this.listFiles.filter((entry) => !this.isSameFile(entry, file));
        if (this.currentFile && this.isSameFile(this.currentFile, file)) {
          this.closeEditor();
        }
        this.showToast('Deleted', 'success');
      } catch (error: any) {
        if (error && error.message === 'Permission denied.') {
          return;
        }
        this.showToast(error && error.message ? error.message : 'Failed to delete file.', 'error');
      }
    }
  }

  private async handleLoadMore() {
    if (this.listLoading || !this.listHasMore) {
      return;
    }
    await this.refreshFileList(false);
  }

  private async handleSave(event: CustomEvent) {
    const currentFile = this.currentFile;
    if (!currentFile) {
      return;
    }
    const content = event.detail?.content || '';
    try {
      await this.saveCurrentFile(currentFile, content);
      this.getEditorModalApi()?.markSaved();
      this.showToast('Saved', 'success');
    } catch (error: any) {
      if (error && error.message === 'Permission denied.') {
        return;
      }
      this.showToast(error && error.message ? error.message : 'Failed to save.', 'error');
    }
  }

  private async handleCloseRequest(event: CustomEvent) {
    const dirty = Boolean(event.detail?.dirty);
    if (!dirty) {
      this.closeEditor();
      return;
    }
    const shouldClose = await this.confirm('You have unsaved changes. Close without saving?', 'Discard changes', 'Keep editing');
    if (shouldClose) {
      this.closeEditor();
    }
  }

  private async handleCompareStart(event: CustomEvent) {
    if (!this.currentFile) {
      const fileFromEvent = event.detail?.file;
      if (fileFromEvent) {
        this.currentFile = fileFromEvent;
      } else {
        return;
      }
    }
    const currentFile = this.currentFile;
    if (!currentFile) {
      return;
    }
    const targetSiteId = event.detail?.targetSiteId ?? null;
    const targetHostName = event.detail?.targetHostName ?? null;
    if (this.isSameFile(currentFile, { virtualPath: currentFile.virtualPath, siteId: targetSiteId, hostName: targetHostName })) {
      this.showToast('Select a different site or hostname to copy to.', 'error');
      return;
    }
    this.compareTargetSiteId = targetSiteId;
    this.compareTargetHostName = targetHostName;
    try {
      const response = await this.fetchFileContent(currentFile.virtualPath, targetSiteId, targetHostName);
      if (response.status === 404) {
        const confirmCopy = await this.confirm('The target site does not have this file yet. Copy it now?', 'Copy now', 'Cancel');
        if (!confirmCopy) {
          this.compareTargetSiteId = null;
          this.compareTargetHostName = null;
          return;
        }
        await this.saveToSite(targetSiteId, targetHostName, currentFile.virtualPath, event.detail?.content || '');
        await this.refreshFileList(true);
        this.closeEditor();
        this.compareTargetSiteId = null;
        this.compareTargetHostName = null;
        return;
      }
      if (!response.ok) {
        this.showToast('Failed to load target file.', 'error');
        this.compareTargetSiteId = null;
        this.compareTargetHostName = null;
        return;
      }
      const content = await response.text();
      this.getEditorModalApi()?.enterDiffMode(content);
    } catch (error: any) {
      if (error && error.message === 'Permission denied.') {
        this.compareTargetSiteId = null;
        this.compareTargetHostName = null;
        return;
      }
      this.showToast(error && error.message ? error.message : 'Failed to load target file.', 'error');
      this.compareTargetSiteId = null;
      this.compareTargetHostName = null;
    }
  }

  private async handleCompareAccept(event: CustomEvent) {
    const currentFile = this.currentFile;
    if (!currentFile || !this.compareTargetSiteId) {
      return;
    }
    const contentToCopy = event.detail?.content || '';
    try {
      await this.saveToSite(this.compareTargetSiteId, this.compareTargetHostName ?? null, currentFile.virtualPath, contentToCopy);
      await this.getEditorModalApi()?.exitDiffMode(false);
      await this.refreshFileList(true);
      this.closeEditor();
      this.compareTargetSiteId = null;
      this.compareTargetHostName = null;
    } catch (error: any) {
      if (error && error.message === 'Permission denied.') {
        this.compareTargetSiteId = null;
        this.compareTargetHostName = null;
        return;
      }
      this.showToast(error && error.message ? error.message : 'Failed to save.', 'error');
    }
  }

  private handleCompareCancel() {
    this.compareTargetSiteId = null;
    this.compareTargetHostName = null;
  }

  private handleEditorError(event: CustomEvent) {
    const message = event.detail?.message || 'An unexpected error occurred.';
    this.showToast(message, 'error');
  }

  private async loadContent(file: { virtualPath: string; siteId: string | null; siteName: string; hostName: string | null }, readOnly: boolean) {
    try {
      const response = await this.fetchFileContent(file.virtualPath, file.siteId, file.hostName);
      if (!response.ok) {
        this.showToast('Failed to load file.', 'error');
        return;
      }
      const content = await response.text();
      this.currentFile = file;
      this.compareTargetSiteId = null;
      this.getEditorModalApi()?.open(file, content, readOnly);
    } catch (error: any) {
      this.showToast(error && error.message ? error.message : 'Failed to load file.', 'error');
    }
  }

  private async fetchFileContent(virtualPath: string, siteId: string | null, hostName: string | null) {
    let url = this.endpoints.fileContentUrl + '?virtualPath=' + encodeURIComponent(virtualPath);
    if (siteId) {
      url += '&siteId=' + encodeURIComponent(siteId);
    }
    if (hostName) {
      url += '&hostName=' + encodeURIComponent(hostName);
    }
    const response = await fetch(url);
    if (this.isPermissionDenied(response)) {
      this.getEditorModalApi()?.showPermissionWarning('You do not have permission to read files.');
      throw new Error('Permission denied.');
    }
    return response;
  }

  private async saveCurrentFile(file: { virtualPath: string; siteId: string | null; hostName: string | null }, content: string) {
    const payload = {
      virtualPath: file.virtualPath,
      siteId: file.siteId,
      hostName: file.hostName,
      content: content
    };
    const response = await fetch(this.endpoints.saveFileUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': this.antiForgeryToken
      },
      body: JSON.stringify(payload)
    });
    if (this.isPermissionDenied(response)) {
      this.getEditorModalApi()?.showPermissionWarning('You do not have permission to save files.');
      throw new Error('Permission denied.');
    }
    if (!response.ok) {
      throw new Error('Failed to save.');
    }
  }

  private async saveToSite(siteId: string | null, hostName: string | null, virtualPath: string, content: string) {
    const payload = {
      virtualPath: virtualPath,
      siteId: siteId,
      hostName: hostName,
      content: content
    };
    const response = await fetch(this.endpoints.saveFileUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': this.antiForgeryToken
      },
      body: JSON.stringify(payload)
    });
    if (this.isPermissionDenied(response)) {
      this.getEditorModalApi()?.showPermissionWarning('You do not have permission to save to the target site.');
      throw new Error('Permission denied.');
    }
    if (!response.ok) {
      throw new Error('Failed to save.');
    }
    this.showToast('Saved', 'success');
  }

  private async checkFileExists(file: { virtualPath: string; siteId: string | null; hostName: string | null }) {
    const response = await this.fetchFileContent(file.virtualPath, file.siteId, file.hostName);
    if (response.status === 404) {
      return false;
    }
    if (!response.ok) {
      throw new Error('Failed to check existing file.');
    }
    return true;
  }

  private async saveNewFile(file: { virtualPath: string; siteId: string | null; hostName: string | null }) {
    const payload = {
      virtualPath: file.virtualPath,
      siteId: file.siteId,
      hostName: file.hostName,
      content: ''
    };
    const response = await fetch(this.endpoints.saveFileUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': this.antiForgeryToken
      },
      body: JSON.stringify(payload)
    });
    if (this.isPermissionDenied(response)) {
      this.getEditorModalApi()?.showPermissionWarning('You do not have permission to create files.');
      throw new Error('Permission denied.');
    }
    if (!response.ok) {
      throw new Error('Failed to create.');
    }
  }

  private async deleteFile(file: { virtualPath: string; siteId: string | null; hostName: string | null }) {
    const payload = {
      virtualPath: file.virtualPath,
      siteId: file.siteId,
      hostName: file.hostName
    };
    const response = await fetch(this.endpoints.deleteFileUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': this.antiForgeryToken
      },
      body: JSON.stringify(payload)
    });
    if (this.isPermissionDenied(response)) {
      this.getEditorModalApi()?.showPermissionWarning('You do not have permission to delete files.');
      throw new Error('Permission denied.');
    }
    if (response.status === 404) {
      return;
    }
    if (!response.ok) {
      throw new Error('Failed to delete.');
    }
  }

  private closeEditor() {
    this.getEditorModalApi()?.close();
    this.currentFile = null;
    this.compareTargetSiteId = null;
    this.compareTargetHostName = null;
  }

  private buildFileListUrl(pageNumber: number) {
    const url = new URL(this.endpoints.fileListUrl, window.location.href);
    url.searchParams.set('pageNumber', String(pageNumber));
    if (this.filterPath) {
      url.searchParams.set('virtualPath', this.filterPath);
    }
    if (this.filterSiteId) {
      url.searchParams.set('siteId', this.filterSiteId);
    }
    if (this.filterHostName) {
      url.searchParams.set('hostName', this.filterHostName);
    }
    return url.toString();
  }

  private async refreshFileList(reset: boolean) {
    if (this.listLoading) {
      return;
    }
    const nextPage = reset ? 1 : this.listPageNumber + 1;
    this.listLoading = true;
    if (reset) {
      this.listPageNumber = 1;
      this.listFiles = [];
      this.listHasMore = true;
    }
    try {
      const response = await fetch(this.buildFileListUrl(nextPage));
      if (!response.ok) {
        this.showToast('Failed to load file list.', 'error');
        return;
      }
      const data = await response.json() as VirtualTextFileListResponse;
      if (reset) {
        this.listFiles = data.files;
      } else {
        this.listFiles = [...this.listFiles, ...data.files];
      }
      this.listHasMore = data.hasMore && data.files.length > 0;
      if (data.files.length > 0) {
        this.listPageNumber = nextPage;
      }
    } catch (error: any) {
      this.showToast(error && error.message ? error.message : 'Failed to load file list.', 'error');
    } finally {
      this.listLoading = false;
    }
  }

  private async confirm(message: string, confirmText: string, cancelText: string) {
    const confirmModal = this.getConfirmModalApi();
    if (!confirmModal) {
      return false;
    }
    return confirmModal.open(message, confirmText, cancelText);
  }

  private getToastApi() {
    const value = this.toastRef.value;
    if (!value) {
      return null;
    }
    return value as unknown as ToastApi;
  }

  private getConfirmModalApi() {
    const value = this.confirmModalRef.value;
    if (!value) {
      return null;
    }
    return value as unknown as ConfirmModalApi;
  }

  private getCreateFormApi() {
    const value = this.createFormRef.value;
    if (!value) {
      return null;
    }
    return value as unknown as CreateFormApi;
  }

  private getEditorModalApi() {
    const value = this.editorModalRef.value;
    if (!value) {
      return null;
    }
    return value as unknown as EditorModalApi;
  }

  private isPermissionDenied(response: Response) {
    return response.status === 401 || response.status === 403 || response.redirected;
  }

  private isSameFile(
    a: { virtualPath: string; siteId: string | null; hostName?: string | null },
    b: { virtualPath: string; siteId: string | null; hostName?: string | null }
  ) {
    return a.virtualPath === b.virtualPath
      && (a.siteId || '') === (b.siteId || '')
      && (a.hostName || '') === (b.hostName || '');
  }
}
