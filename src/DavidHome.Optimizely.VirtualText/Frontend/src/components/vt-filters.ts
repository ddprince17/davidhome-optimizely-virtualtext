import { LitElement, html } from 'lit';
import { customElement, property } from 'lit/decorators.js';
import type { VirtualTextSiteOption } from './types';

@customElement('vt-filters')
export class VtFilters extends LitElement {
  static properties = {
    sites: { attribute: false }
  };

  @property({ attribute: false }) accessor sites: VirtualTextSiteOption[] = [];

  createRenderRoot() {
    return this;
  }

  private refreshTimer: number | null = null;

  private emitChange() {
    const pathInput = this.querySelector<HTMLInputElement>('#vt-filter-path');
    const siteSelect = this.querySelector<HTMLSelectElement>('#vt-filter-site');
    const path = pathInput ? pathInput.value.trim() : '';
    const siteId = siteSelect ? siteSelect.value || null : null;
    this.dispatchEvent(new CustomEvent('vt-filter-change', { detail: { path, siteId }, bubbles: true, composed: true }));
  }

  private handlePathInput() {
    if (this.refreshTimer) {
      window.clearTimeout(this.refreshTimer);
    }
    this.refreshTimer = window.setTimeout(() => {
      this.emitChange();
      this.refreshTimer = null;
    }, 300);
  }

  private handleSiteChange() {
    this.emitChange();
  }

  render() {
    return html`
      <div class="vt-filter flex flex-wrap items-end gap-3">
        <label class="flex basis-1/2 flex-col gap-1 text-sm">
          Search path
          <input type="text" id="vt-filter-path" placeholder="example.txt" class="w-full min-w-[220px] rounded-md border border-slate-300 px-3 py-2 text-sm shadow-sm focus:border-slate-400 focus:outline-none focus:ring-2 focus:ring-slate-200" @input=${this.handlePathInput} />
        </label>
        <label class="flex flex-col gap-1 text-sm">
          Filter site
          <select id="vt-filter-site" class="min-w-[220px] rounded-md border border-slate-300 px-3 py-2 text-sm shadow-sm focus:border-slate-400 focus:outline-none focus:ring-2 focus:ring-slate-200" @change=${this.handleSiteChange}>
            ${this.sites.map((site) => html`<option value=${site.siteId || ''}>${site.name}</option>`)}
          </select>
        </label>
      </div>
    `;
  }
}
