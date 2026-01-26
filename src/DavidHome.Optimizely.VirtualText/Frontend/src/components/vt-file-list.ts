import { LitElement, html } from 'lit';
import { customElement, property } from 'lit/decorators.js';
import type { VirtualTextFileListItem } from './types';

@customElement('vt-file-list')
export class VtFileList extends LitElement {
  static properties = {
    files: { attribute: false },
    canEdit: { type: Boolean },
    hasMore: { type: Boolean },
    loading: { type: Boolean }
  };

  @property({ attribute: false }) accessor files: VirtualTextFileListItem[] = [];
  @property({ type: Boolean }) accessor canEdit = false;
  @property({ type: Boolean }) accessor hasMore = true;
  @property({ type: Boolean }) accessor loading = false;

  createRenderRoot() {
    return this;
  }

  private handleAction(action: 'view' | 'edit' | 'delete', file: VirtualTextFileListItem) {
    this.dispatchEvent(new CustomEvent('vt-file-action', { detail: { action, file }, bubbles: true, composed: true }));
  }

  private handleLoadMore() {
    if (this.loading || !this.hasMore) {
      return;
    }
    this.dispatchEvent(new CustomEvent('vt-load-more', { bubbles: true, composed: true }));
  }

  render() {
    return html`
      <table class="vt-table w-full table-auto border-collapse text-sm">
        <thead>
          <tr class="border-b border-slate-200 bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
            <th class="px-3 py-2">Path</th>
            <th class="px-3 py-2">Site</th>
            <th class="px-3 py-2">Actions</th>
          </tr>
        </thead>
        <tbody id="vt-file-list" class="divide-y divide-slate-100">
          ${this.files.map((file) => html`
            <tr data-virtual-path=${file.virtualPath} data-site-id=${file.siteId || ''}>
              <td class="px-3 py-2">${file.virtualPath}</td>
              <td class="px-3 py-2">${file.siteName}</td>
              <td class="px-3 py-2">
                <div class="flex items-center gap-2">
                  <button type="button" class="vt-action rounded-md border border-slate-300 px-2 py-1 text-xs font-medium text-slate-700 hover:bg-slate-50" data-action="view" @click=${() => this.handleAction('view', file)}>View</button>
                  ${this.canEdit
                    ? html`
                      <button type="button" class="vt-action rounded-md border border-slate-300 px-2 py-1 text-xs font-medium text-slate-700 hover:bg-slate-50" data-action="edit" @click=${() => this.handleAction('edit', file)}>Edit</button>
                      <button type="button" class="vt-action rounded-md border border-rose-200 px-2 py-1 text-xs font-medium text-rose-700 hover:bg-rose-50" data-action="delete" @click=${() => this.handleAction('delete', file)}>Delete</button>
                    `
                    : html``}
                </div>
              </td>
            </tr>
          `)}
        </tbody>
      </table>
      <div class="vt-load-more flex justify-center pt-3" ?hidden=${!this.hasMore}>
        <button type="button" id="vt-load-more" class="rounded-md border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50" ?disabled=${this.loading} @click=${this.handleLoadMore}>Load more</button>
      </div>
    `;
  }
}
