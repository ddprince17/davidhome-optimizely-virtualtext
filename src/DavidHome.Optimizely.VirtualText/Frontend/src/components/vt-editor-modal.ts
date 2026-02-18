import { LitElement, html } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import type { VirtualTextSiteOption } from './types';

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

function getVirtualTextMonaco() {
  if (!window.VirtualTextMonaco) {
    window.VirtualTextMonaco = {
      createEditor: createEditor,
      monaco: null
    };
  }
  return window.VirtualTextMonaco;
}

window.VirtualTextMonaco = getVirtualTextMonaco();

@customElement('vt-editor-modal')
export class VtEditorModal extends LitElement {
  static properties = {
    sites: { attribute: false }
  };

  @property({ attribute: false }) accessor sites: VirtualTextSiteOption[] = [];

  private editor: MonacoEditor | null = null;
  private diffEditor: MonacoDiffEditor | null = null;
  private diffOriginal: MonacoModel | null = null;
  private diffModified: MonacoModel | null = null;
  private editorContentBeforeDiff: string | null = null;
  private dirtyBeforeDiff = false;
  private isDirty = false;
  private isReadOnly = false;
  private isCompareMode = false;
  private currentFile: { virtualPath: string; siteId: string | null; siteName: string; hostName: string | null } | null = null;
  private keydownHandler: ((event: KeyboardEvent) => void) | null = null;
  @state() accessor compareSiteId: string | null = null;
  @state() accessor compareHostName: string | null = null;

  createRenderRoot() {
    return this;
  }

  protected updated(changed: Map<string, unknown>) {
    if (changed.has('sites') && !this.compareSiteId && this.sites.length > 0) {
      this.compareSiteId = this.sites[0]?.siteId ?? null;
    }
  }

  open(file: { virtualPath: string; siteId: string | null; siteName: string; hostName: string | null }, content: string, readOnly: boolean) {
    this.currentFile = file;
    this.isReadOnly = readOnly;
    if (!this.compareSiteId) {
      this.compareSiteId = this.sites[0]?.siteId ?? null;
    }
    this.compareHostName = null;
    this.setHeader(file);
    this.setCompareMode(false);
    this.setPermissionWarning(false);
    this.setDirty(false);
    this.setLoading(true);
    this.showModal();

    monacoLoadingStart = () => this.setLoading(true);
    monacoLoadingEnd = () => this.setLoading(false);

    const editorContainer = this.getEditorContainer();
    if (!editorContainer) {
      this.setLoading(false);
      this.emitError('Editor container not found.');
      return;
    }
    editorContainer.innerHTML = '';

    createEditor(editorContainer, content || '', {
      language: 'plaintext',
      readOnly: this.isReadOnly
    }).then((created) => {
      this.editor = created;
      this.setLoading(false);
      this.setSaveDisabled(this.isReadOnly);
      this.setCompareDisabled(this.isReadOnly);
      if (!this.isReadOnly) {
        this.editor.onDidChangeModelContent(() => this.setDirty(true));
      }
      this.addKeyBindings();
    }).catch((error: any) => {
      this.setLoading(false);
      this.emitError(error && error.message ? error.message : 'Failed to load editor.');
    });
  }

  close() {
    this.hideModal();
    this.disposeEditors();
    this.isDirty = false;
    this.isReadOnly = false;
    this.currentFile = null;
    this.editorContentBeforeDiff = null;
    this.dirtyBeforeDiff = false;
    this.isCompareMode = false;
    this.setCompareMode(false);
    this.setPermissionWarning(false);
    this.setSaveDisabled(false);
    this.setCompareDisabled(false);
    this.removeKeyBindings();
    monacoLoadingStart = null;
    monacoLoadingEnd = null;
    const editorContainer = this.getEditorContainer();
    if (editorContainer) {
      editorContainer.innerHTML = '';
    }
  }

  markSaved() {
    this.setDirty(false);
  }

  showPermissionWarning(message: string) {
    const warning = this.querySelector<HTMLDivElement>('#vt-permission-warning');
    if (!warning) {
      return;
    }
    const text = warning.querySelector('span');
    if (text) {
      text.textContent = message;
    }
    warning.hidden = false;
  }

  enterDiffMode(sourceContent: string) {
    if (!this.editor || !monaco) {
      return;
    }
    this.editorContentBeforeDiff = this.editor.getValue();
    this.dirtyBeforeDiff = this.isDirty;
    this.disposeEditors();
    const editorContainer = this.getEditorContainer();
    if (!editorContainer) {
      return;
    }
    this.diffOriginal = monaco.editor.createModel(sourceContent || '', 'plaintext');
    this.diffModified = monaco.editor.createModel(this.editorContentBeforeDiff || '', 'plaintext');
    this.diffEditor = monaco.editor.createDiffEditor(editorContainer, {
      readOnly: false,
      originalEditable: false,
      automaticLayout: true
    });
    this.diffEditor.setModel({
      original: this.diffOriginal,
      modified: this.diffModified
    });
    this.isCompareMode = true;
    this.setCompareMode(true);
    this.setSaveDisabled(true);
  }

  async exitDiffMode(applyChanges: boolean) {
    const contentToApply = applyChanges && this.diffModified ? this.diffModified.getValue() : this.editorContentBeforeDiff;
    this.disposeEditors();
    const editorContainer = this.getEditorContainer();
    if (!editorContainer) {
      this.emitError('Editor container not found.');
      return;
    }
    editorContainer.innerHTML = '';
    try {
      this.editor = await createEditor(editorContainer, contentToApply || '', {
        language: 'plaintext',
        readOnly: this.isReadOnly
      });
      this.setDirty(applyChanges || this.dirtyBeforeDiff);
      if (!this.isReadOnly) {
        this.editor.onDidChangeModelContent(() => this.setDirty(true));
      }
      this.editorContentBeforeDiff = null;
      this.dirtyBeforeDiff = false;
      this.isCompareMode = false;
      this.setCompareMode(false);
      this.setSaveDisabled(this.isReadOnly);
    } catch (error: any) {
      this.emitError(error && error.message ? error.message : 'Failed to load editor.');
    }
  }

  private handleSave() {
    if (!this.editor || this.isReadOnly || this.isCompareMode) {
      return;
    }
    this.dispatchEvent(new CustomEvent('vt-save', { detail: { content: this.editor.getValue() }, bubbles: true, composed: true }));
  }

  private handleCloseRequest() {
    this.dispatchEvent(new CustomEvent('vt-close-request', { detail: { dirty: this.isDirty }, bubbles: true, composed: true }));
  }

  private handleBackdropClick(event: Event) {
    if (event.target instanceof HTMLElement && event.target.classList.contains('vt-modal-backdrop')) {
      this.handleCloseRequest();
    }
  }

  private handleCompareStart() {
    if (this.isReadOnly) {
      return;
    }
    const select = this.querySelector<HTMLSelectElement>('#vt-compare-site');
    const hostSelect = this.querySelector<HTMLSelectElement>('#vt-compare-host');
    const targetSiteId = select ? select.value || null : null;
    const targetHostName = hostSelect ? hostSelect.value || null : null;
    this.dispatchEvent(new CustomEvent('vt-compare-start', { detail: { targetSiteId, targetHostName, file: this.currentFile, content: this.editor?.getValue() || '' }, bubbles: true, composed: true }));
  }

  private handleCompareSiteChange(event: Event) {
    const target = event.currentTarget as HTMLSelectElement | null;
    this.compareSiteId = target ? target.value || null : null;
    this.compareHostName = null;
  }

  private handleCompareHostChange(event: Event) {
    const target = event.currentTarget as HTMLSelectElement | null;
    this.compareHostName = target ? target.value || null : null;
  }

  private handleCompareAccept() {
    if (!this.diffModified) {
      return;
    }
    this.dispatchEvent(new CustomEvent('vt-compare-accept', { detail: { content: this.diffModified.getValue() }, bubbles: true, composed: true }));
  }

  private handleCompareCancel() {
    this.exitDiffMode(false).then(() => {
      this.dispatchEvent(new CustomEvent('vt-compare-cancel', { bubbles: true, composed: true }));
    });
  }

  private handlePermissionClose() {
    this.setPermissionWarning(false);
  }

  private emitError(message: string) {
    this.dispatchEvent(new CustomEvent('vt-error', { detail: { message }, bubbles: true, composed: true }));
  }

  private addKeyBindings() {
    this.removeKeyBindings();
    this.keydownHandler = (event: KeyboardEvent) => {
      const modal = this.getModal();
      if (!modal || modal.getAttribute('aria-hidden') === 'true') {
        return;
      }
      const confirmModal = document.getElementById('vt-confirm-modal');
      if (confirmModal && confirmModal.getAttribute('aria-hidden') === 'false') {
        return;
      }
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') {
        event.preventDefault();
        this.handleSave();
      }
      if (event.key === 'Escape') {
        event.preventDefault();
        this.handleCloseRequest();
      }
    };
    window.addEventListener('keydown', this.keydownHandler);
  }

  private removeKeyBindings() {
    if (this.keydownHandler) {
      window.removeEventListener('keydown', this.keydownHandler);
      this.keydownHandler = null;
    }
  }

  private setDirty(dirty: boolean) {
    this.isDirty = dirty;
    const title = this.querySelector<HTMLDivElement>('#vt-editor-title');
    if (!title) {
      return;
    }
    title.textContent = this.currentFile
      ? (dirty ? this.currentFile.virtualPath + ' *' : this.currentFile.virtualPath)
      : 'Editor';
  }

  private setHeader(file: { virtualPath: string; siteId: string | null; siteName: string; hostName: string | null }) {
    const title = this.querySelector<HTMLDivElement>('#vt-editor-title');
    const subtitle = this.querySelector<HTMLDivElement>('#vt-editor-subtitle');
    if (title) {
      title.textContent = file.virtualPath;
    }
    if (subtitle) {
      const siteLabel = file.siteName || 'Default (All Sites)';
      const hostLabel = file.hostName || 'Default (All Hostnames)';
      subtitle.textContent = `${siteLabel} · ${hostLabel}`;
    }
  }

  private setLoading(loading: boolean) {
    const loadingEl = this.querySelector<HTMLDivElement>('#vt-editor-loading');
    if (!loadingEl) {
      return;
    }
    loadingEl.hidden = !loading;
  }

  private setCompareMode(enabled: boolean) {
    const accept = this.querySelector<HTMLButtonElement>('#vt-compare-accept');
    const cancel = this.querySelector<HTMLButtonElement>('#vt-compare-cancel');
    const start = this.querySelector<HTMLButtonElement>('#vt-compare-start');
    const select = this.querySelector<HTMLSelectElement>('#vt-compare-site');
    const hostSelect = this.querySelector<HTMLSelectElement>('#vt-compare-host');
    if (accept) {
      accept.hidden = !enabled;
    }
    if (cancel) {
      cancel.hidden = !enabled;
    }
    if (start) {
      start.hidden = enabled;
    }
    if (select) {
      select.disabled = enabled || this.isReadOnly;
    }
    if (hostSelect) {
      hostSelect.disabled = enabled || this.isReadOnly || !this.compareSiteId;
    }
  }

  private setSaveDisabled(disabled: boolean) {
    const save = this.querySelector<HTMLButtonElement>('#vt-save');
    if (save) {
      save.disabled = disabled;
    }
  }

  private setCompareDisabled(disabled: boolean) {
    const start = this.querySelector<HTMLButtonElement>('#vt-compare-start');
    const select = this.querySelector<HTMLSelectElement>('#vt-compare-site');
    const hostSelect = this.querySelector<HTMLSelectElement>('#vt-compare-host');
    if (start) {
      start.disabled = disabled;
    }
    if (select) {
      select.disabled = disabled || this.isCompareMode;
    }
    if (hostSelect) {
      hostSelect.disabled = disabled || this.isCompareMode || !this.compareSiteId;
    }
  }

  private setPermissionWarning(visible: boolean) {
    const warning = this.querySelector<HTMLDivElement>('#vt-permission-warning');
    if (!warning) {
      return;
    }
    warning.hidden = !visible;
  }

  private showModal() {
    const modal = this.getModal();
    if (!modal) {
      return;
    }
    modal.classList.remove('hidden');
    modal.hidden = false;
    modal.setAttribute('aria-hidden', 'false');
  }

  private hideModal() {
    const modal = this.getModal();
    if (!modal) {
      return;
    }
    modal.classList.add('hidden');
    modal.hidden = true;
    modal.setAttribute('aria-hidden', 'true');
  }

  private getModal() {
    return this.querySelector<HTMLDivElement>('#vt-editor-modal');
  }

  private getEditorContainer() {
    return this.querySelector<HTMLDivElement>('#vt-editor');
  }

  private disposeEditors() {
    if (this.diffEditor) {
      this.diffEditor.dispose();
      this.diffEditor = null;
    }
    if (this.diffOriginal) {
      this.diffOriginal.dispose();
      this.diffOriginal = null;
    }
    if (this.diffModified) {
      this.diffModified.dispose();
      this.diffModified = null;
    }
    if (this.editor) {
      this.editor.dispose();
      this.editor = null;
    }
  }

  render() {
    const selectedSite = this.sites.find((site) => (site.siteId || '') === (this.compareSiteId || ''));
    const hostOptions = selectedSite?.hosts ?? [];
    const hostEnabled = Boolean(this.compareSiteId);

    return html`
      <div class="vt-modal fixed inset-0 z-[2000] hidden flex items-center justify-center" id="vt-editor-modal" aria-hidden="true" hidden @click=${this.handleBackdropClick}>
        <div class="vt-modal-backdrop absolute inset-0 bg-slate-900/60"></div>
        <div class="vt-modal-content relative z-10 flex h-[88vh] w-[92vw] max-w-5xl flex-col overflow-hidden rounded-xl bg-white shadow-2xl">
          <div class="vt-modal-header flex flex-col gap-3 border-b border-slate-200 bg-slate-50 px-4 py-3">
            <div class="vt-modal-header-row flex flex-wrap items-center justify-between gap-3">
              <div>
                <div id="vt-editor-title" class="vt-title text-base font-semibold text-slate-900">Editor</div>
                <div id="vt-editor-subtitle" class="vt-subtitle text-xs text-slate-500"></div>
              </div>
              <div class="vt-modal-actions flex items-center gap-2">
                <button type="button" id="vt-save" class="vt-primary rounded-md bg-slate-900 px-3 py-1.5 text-sm font-semibold text-white shadow hover:bg-slate-800" @click=${this.handleSave}>Save</button>
                <button type="button" id="vt-close" class="rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100" @click=${this.handleCloseRequest}>Close</button>
              </div>
            </div>
            <div class="vt-compare flex flex-wrap items-end gap-3">
              <label class="flex flex-col gap-1 text-sm">
                Copy to site
                <select id="vt-compare-site" class="min-w-[220px] rounded-md border border-slate-300 px-3 py-2 text-sm shadow-sm focus:border-slate-400 focus:outline-none focus:ring-2 focus:ring-slate-200" @change=${this.handleCompareSiteChange}>
                  ${this.sites.map(
                    (site) => html`<option value=${site.siteId || ''} ?selected=${(site.siteId || '') === (this.compareSiteId || '')}>${site.name}</option>`
                  )}
                </select>
              </label>
              <label class="flex flex-col gap-1 text-sm">
                Copy to hostname
                <select id="vt-compare-host" class="min-w-[220px] rounded-md border border-slate-300 px-3 py-2 text-sm shadow-sm focus:border-slate-400 focus:outline-none focus:ring-2 focus:ring-slate-200" ?disabled=${!hostEnabled} @change=${this.handleCompareHostChange}>
                  <option value="">Default (All Hostnames)</option>
                  ${hostOptions.map((host) => html`<option value=${host} ?selected=${host === (this.compareHostName || '')}>${host}</option>`)}
                </select>
              </label>
              <button type="button" id="vt-compare-start" class="vt-primary rounded-md bg-slate-900 px-3 py-1.5 text-sm font-semibold text-white shadow hover:bg-slate-800" @click=${this.handleCompareStart}>Preview Copy</button>
              <div class="vt-compare-actions flex items-center gap-2">
                <button type="button" id="vt-compare-accept" class="vt-primary rounded-md bg-slate-900 px-3 py-1.5 text-sm font-semibold text-white shadow hover:bg-slate-800" hidden @click=${this.handleCompareAccept}>Copy &amp; Save</button>
                <button type="button" id="vt-compare-cancel" class="rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100" hidden @click=${this.handleCompareCancel}>Cancel</button>
              </div>
            </div>
            <div id="vt-permission-warning" class="vt-alert hidden rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">
              <span>You do not have permission to save files.</span>
              <button type="button" class="vt-alert-close ml-2 text-base font-semibold leading-none text-rose-700" aria-label="Dismiss" @click=${this.handlePermissionClose}>×</button>
            </div>
          </div>
          <div id="vt-editor" class="vt-editor relative min-h-[400px] flex-1">
            <div id="vt-editor-loading" class="vt-editor-loading absolute inset-0 z-10 flex flex-col items-center justify-center gap-3 bg-white/80 text-center" hidden>
              <div class="vt-spinner h-9 w-9 animate-spin rounded-full border-2 border-slate-200 border-t-slate-900" aria-hidden="true"></div>
              <div class="vt-loading-text text-xs text-slate-600" role="status" aria-live="polite">Loading editor...</div>
            </div>
          </div>
        </div>
      </div>
    `;
  }
}
