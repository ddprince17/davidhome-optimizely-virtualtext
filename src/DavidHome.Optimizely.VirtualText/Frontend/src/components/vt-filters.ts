import { LitElement, html } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import type { VirtualTextSiteOption } from './types';

@customElement('vt-filters')
export class VtFilters extends LitElement {
  static properties = {
    sites: { attribute: false }
  };

  @property({ attribute: false }) accessor sites: VirtualTextSiteOption[] = [];
  @state() accessor selectedSiteId: string | null = null;
  @state() accessor selectedHostName: string | null = null;

  createRenderRoot() {
    return this;
  }

  private refreshTimer: number | null = null;

  protected updated(changed: Map<string, unknown>) {
    if (changed.has('sites') && this.selectedSiteId === null && this.sites.length > 0) {
      this.selectedSiteId = this.sites[0]?.siteId ?? null;
    }
  }

  private emitChange() {
    const pathInput = this.querySelector<HTMLInputElement>('#vt-filter-path');
    const siteSelect = this.querySelector<HTMLSelectElement>('#vt-filter-site');
    const hostSelect = this.querySelector<HTMLSelectElement>('#vt-filter-host');
    const path = pathInput ? pathInput.value.trim() : '';
    const siteId = siteSelect ? siteSelect.value || null : null;
    const hostName = hostSelect ? hostSelect.value || null : null;
    this.dispatchEvent(new CustomEvent('vt-filter-change', { detail: { path, siteId, hostName }, bubbles: true, composed: true }));
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

  private handleSiteChange(event: Event) {
    const target = event.currentTarget as HTMLSelectElement | null;
    this.selectedSiteId = target ? target.value || null : null;
    this.selectedHostName = null;
    this.emitChange();
  }

  private handleHostChange(event: Event) {
    const target = event.currentTarget as HTMLSelectElement | null;
    this.selectedHostName = target ? target.value || null : null;
    this.emitChange();
  }

  render() {
    const selectedSite = this.sites.find((site) => (site.siteId || '') === (this.selectedSiteId || ''));
    const hostOptions = selectedSite?.hosts ?? [];
    const hostEnabled = Boolean(this.selectedSiteId);

    return html`
      <div class="vt-filter flex flex-wrap items-end gap-3">
        <label class="flex basis-1/2 flex-col gap-1 text-sm">
          Search path
          <input type="text" id="vt-filter-path" placeholder="example.txt" class="w-full min-w-[220px] rounded-md border border-slate-300 px-3 py-2 text-sm shadow-sm focus:border-slate-400 focus:outline-none focus:ring-2 focus:ring-slate-200" @input=${this.handlePathInput} />
        </label>
        <label class="flex flex-col gap-1 text-sm">
          Filter site
          <select id="vt-filter-site" class="min-w-[220px] rounded-md border border-slate-300 px-3 py-2 text-sm shadow-sm focus:border-slate-400 focus:outline-none focus:ring-2 focus:ring-slate-200" @change=${this.handleSiteChange}>
            ${this.sites.map(
              (site) => html`<option value=${site.siteId || ''} ?selected=${(site.siteId || '') === (this.selectedSiteId || '')}>${site.name}</option>`
            )}
          </select>
        </label>
        <label class="flex flex-col gap-1 text-sm">
          Filter hostname
          <select id="vt-filter-host" class="min-w-[220px] rounded-md border border-slate-300 px-3 py-2 text-sm shadow-sm focus:border-slate-400 focus:outline-none focus:ring-2 focus:ring-slate-200" ?disabled=${!hostEnabled} @change=${this.handleHostChange}>
            <option value="">Default (All Hostnames)</option>
            ${hostOptions.map((host) => html`<option value=${host} ?selected=${host === (this.selectedHostName || '')}>${host}</option>`)}
          </select>
        </label>
      </div>
    `;
  }
}
