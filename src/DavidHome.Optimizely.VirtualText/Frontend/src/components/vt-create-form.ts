import { LitElement, html } from 'lit';
import { customElement, property } from 'lit/decorators.js';
import type { VirtualTextSiteOption } from './types';

@customElement('vt-create-form')
export class VtCreateForm extends LitElement {
  static properties = {
    sites: { attribute: false },
    canEdit: { type: Boolean }
  };

  @property({ attribute: false }) accessor sites: VirtualTextSiteOption[] = [];
  @property({ type: Boolean }) accessor canEdit = false;

  createRenderRoot() {
    return this;
  }

  reset() {
    const pathInput = this.querySelector<HTMLInputElement>('#vt-new-path');
    if (pathInput) {
      pathInput.value = '';
    }
  }

  private handleCreate() {
    const pathInput = this.querySelector<HTMLInputElement>('#vt-new-path');
    const siteSelect = this.querySelector<HTMLSelectElement>('#vt-new-site');
    const virtualPath = pathInput?.value.trim() || '';
    if (!virtualPath) {
      this.dispatchEvent(new CustomEvent('vt-create-error', { detail: { message: 'Please enter a virtual path.' }, bubbles: true, composed: true }));
      return;
    }
    const siteId = siteSelect ? siteSelect.value || null : null;
    const siteName = siteSelect ? siteSelect.options[siteSelect.selectedIndex]?.text || '' : '';
    this.dispatchEvent(new CustomEvent('vt-create', { detail: { virtualPath, siteId, siteName }, bubbles: true, composed: true }));
  }

  render() {
    if (!this.canEdit) {
      return html``;
    }

    return html`
      <div class="vt-create flex flex-wrap items-end gap-3">
        <label class="flex flex-col gap-1 text-sm">
          Path
          <input type="text" id="vt-new-path" placeholder="example.txt" class="min-w-[220px] rounded-md border border-slate-300 px-3 py-2 text-sm shadow-sm focus:border-slate-400 focus:outline-none focus:ring-2 focus:ring-slate-200" />
        </label>
        <label class="flex flex-col gap-1 text-sm">
          Site
          <select id="vt-new-site" class="min-w-[220px] rounded-md border border-slate-300 px-3 py-2 text-sm shadow-sm focus:border-slate-400 focus:outline-none focus:ring-2 focus:ring-slate-200">
            ${this.sites.map((site) => html`<option value=${site.siteId || ''}>${site.name}</option>`)}
          </select>
        </label>
        <button type="button" id="vt-create-file" class="rounded-md bg-slate-900 px-4 py-2 text-sm font-semibold text-white shadow hover:bg-slate-800" @click=${this.handleCreate}>Create</button>
      </div>
    `;
  }
}
