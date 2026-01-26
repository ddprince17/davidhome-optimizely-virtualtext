import { LitElement, html } from 'lit';
import { customElement, state } from 'lit/decorators.js';
import { createRef, ref } from 'lit/directives/ref.js';
import type { VirtualTextImportItem, VirtualTextImportListResponse, VirtualTextSiteOption } from './types';
import './vt-toast-container';

type ToastApi = {
  show: (message: string, type?: 'success' | 'error' | 'info') => void;
};

type ImportBootstrap = {
  endpoints: {
    importFileUrl: string;
    importListUrl: string;
  };
  sites: VirtualTextSiteOption[];
  items: VirtualTextImportItem[];
  canEdit: boolean;
  antiForgeryToken: string;
};

@customElement('vt-import-app')
export class VtImportApp extends LitElement {
  private bootstrap: ImportBootstrap = {
    endpoints: {
      importFileUrl: '',
      importListUrl: ''
    },
    sites: [],
    items: [],
    canEdit: false,
    antiForgeryToken: ''
  };

  private endpoints = this.bootstrap.endpoints;
  private antiForgeryToken = '';
  private listPageNumber = 1;
  private toastRef = createRef<HTMLElement>();

  @state() accessor items: VirtualTextImportItem[] = [];
  @state() accessor sites: VirtualTextSiteOption[] = [];
  @state() accessor canEdit = false;
  @state() accessor importing = new Set<string>();
  @state() accessor importingAll = false;
  @state() accessor listHasMore = true;
  @state() accessor listLoading = false;

  createRenderRoot() {
    return this;
  }

  connectedCallback() {
    super.connectedCallback();
    this.bootstrap = this.parseBootstrap();
    this.endpoints = this.bootstrap.endpoints;
    this.antiForgeryToken = this.bootstrap.antiForgeryToken || '';
    this.items = this.bootstrap.items || [];
    this.sites = this.bootstrap.sites || [];
    this.canEdit = this.bootstrap.canEdit;
  }

  firstUpdated() {
    void this.loadPage(true);
  }

  render() {
    return html`
      <div class="vt-wrapper flex flex-col gap-4 font-sans text-slate-900">
        <div class="rounded-xl bg-white px-6 py-6 shadow-sm">
          <div class="mb-4 flex flex-wrap items-center justify-between gap-3 text-sm text-slate-600">
            <div>
              Import file locations from storage by confirming the target site for each entry.
            </div>
            <button
              type="button"
              class="rounded-md bg-slate-900 px-4 py-2 text-sm font-semibold text-white shadow hover:bg-slate-800 disabled:opacity-60"
              ?disabled=${!this.canEdit || this.items.length === 0 || this.importingAll}
              @click=${this.handleImportAll}>
              ${this.importingAll ? 'Importing...' : 'Import all'}
            </button>
          </div>
          <table class="vt-table w-full table-auto border-collapse text-sm">
            <thead>
              <tr class="border-b border-slate-200 bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                <th class="px-3 py-2">Virtual path</th>
                <th class="px-3 py-2">Website</th>
                <th class="px-3 py-2">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              ${this.items.map((item) => this.renderRow(item))}
            </tbody>
          </table>
          ${this.items.length === 0 ? html`
            <div class="py-6 text-center text-sm text-slate-500">No files found to import.</div>
          ` : html``}
          <div class="flex justify-center pt-4" ?hidden=${!this.listHasMore}>
            <button
              type="button"
              class="rounded-md border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-60"
              ?disabled=${this.listLoading}
              @click=${() => this.loadPage(false)}>
              ${this.listLoading ? 'Loading...' : 'Load more'}
            </button>
          </div>
        </div>
      </div>
      <vt-toast-container ${ref(this.toastRef)}></vt-toast-container>
    `;
  }

  private renderRow(item: VirtualTextImportItem) {
    const isImporting = this.importing.has(this.getRowKey(item));
    return html`
      <tr>
        <td class="px-3 py-2">${item.virtualPath}</td>
        <td class="px-3 py-2">
          <select
            class="min-w-[220px] rounded-md border border-slate-300 px-3 py-2 text-sm shadow-sm focus:border-slate-400 focus:outline-none focus:ring-2 focus:ring-slate-200"
            ?disabled=${!this.canEdit}
            @change=${(event: Event) => this.handleSiteChange(item, event)}>
            ${item.isUnknownSite ? html`<option value=${item.sourceSiteId || ''} ?selected=${(item.selectedSiteId || '') === (item.sourceSiteId || '')}>Unknown</option>` : html``}
            ${this.sites.map((site) => html`
              <option value=${site.siteId || ''} ?selected=${(item.selectedSiteId || '') === (site.siteId || '')}>${site.name}</option>
            `)}
          </select>
        </td>
        <td class="px-3 py-2">
          <button
            type="button"
            class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-semibold text-white shadow hover:bg-slate-800 disabled:opacity-60"
            ?disabled=${!this.canEdit || isImporting}
            @click=${() => this.handleImport(item)}>
            ${isImporting ? 'Importing...' : 'Import'}
          </button>
        </td>
      </tr>
    `;
  }

  private handleSiteChange(item: VirtualTextImportItem, event: Event) {
    const target = event.target as HTMLSelectElement;
    const selected = target.value || null;
    this.items = this.items.map((entry) => {
      if (this.getRowKey(entry) !== this.getRowKey(item)) {
        return entry;
      }
      return {
        ...entry,
        selectedSiteId: selected
      };
    });
  }

  private async handleImport(item: VirtualTextImportItem) {
    await this.importEntry(item);
  }

  private async handleImportAll() {
    if (!this.canEdit || this.items.length === 0 || this.importingAll) {
      return;
    }

    this.importingAll = true;
    const snapshot = [...this.items];

    for (const item of snapshot) {
      await this.importEntry(item);
    }

    this.importingAll = false;
  }

  private async importEntry(item: VirtualTextImportItem) {
    if (!this.canEdit) {
      return;
    }
    const rowKey = this.getRowKey(item);
    if (this.importing.has(rowKey)) {
      return;
    }

    this.importing = new Set(this.importing).add(rowKey);
    const payload = {
      virtualPath: item.virtualPath,
      sourceSiteId: item.sourceSiteId,
      targetSiteId: item.selectedSiteId
    };

    try {
      const response = await fetch(this.endpoints.importFileUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'RequestVerificationToken': this.antiForgeryToken
        },
        body: JSON.stringify(payload)
      });

      if (!response.ok) {
        throw new Error('Failed to import file.');
      }

      this.items = this.items.filter((entry) => this.getRowKey(entry) !== rowKey);
      this.showToast('Imported', 'success');
    } catch (error: any) {
      this.showToast(error && error.message ? error.message : 'Failed to import file.', 'error');
    } finally {
      const updated = new Set(this.importing);
      updated.delete(rowKey);
      this.importing = updated;
    }
  }

  private async loadPage(reset: boolean) {
    if (this.listLoading) {
      return;
    }

    const nextPage = reset ? 1 : this.listPageNumber + 1;
    this.listLoading = true;

    try {
      const url = new URL(this.endpoints.importListUrl, window.location.href);
      url.searchParams.set('pageNumber', String(nextPage));
      const response = await fetch(url.toString());
      if (!response.ok) {
        throw new Error('Failed to load import list.');
      }

      const data = await response.json() as VirtualTextImportListResponse;
      this.items = reset ? data.items : [...this.items, ...data.items];
      this.listHasMore = data.hasMore;
      if (data.items.length > 0 || data.hasMore) {
        this.listPageNumber = nextPage;
      }
    } catch (error: any) {
      this.showToast(error && error.message ? error.message : 'Failed to load import list.', 'error');
    } finally {
      this.listLoading = false;
    }
  }

  private getRowKey(item: VirtualTextImportItem) {
    return `${item.virtualPath}::${item.sourceSiteId || 'default'}`;
  }

  private showToast(message: string, type: 'success' | 'error' | 'info' = 'info') {
    const toast = this.getToastApi();
    if (!toast) {
      return;
    }
    toast.show(message, type);
  }

  private getToastApi() {
    const value = this.toastRef.value;
    if (!value) {
      return null;
    }
    return value as unknown as ToastApi;
  }

  private parseBootstrap(): ImportBootstrap {
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
      return JSON.parse(text) as ImportBootstrap;
    } catch {
      return this.bootstrap;
    }
  }
}
