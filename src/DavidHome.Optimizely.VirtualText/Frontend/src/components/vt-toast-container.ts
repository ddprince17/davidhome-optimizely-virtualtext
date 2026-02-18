import { LitElement, html } from 'lit';
import { customElement } from 'lit/decorators.js';

type ToastType = 'success' | 'error' | 'info';

@customElement('vt-toast-container')
export class VtToastContainer extends LitElement {
  createRenderRoot() {
    return this;
  }

  show(message: string, type: ToastType = 'info') {
    const container = this.querySelector<HTMLDivElement>('#vt-notification-container');
    if (!container) {
      return;
    }
    const toast = document.createElement('div');
    const typeClass = type === 'success'
      ? 'border-emerald-200 bg-emerald-50 text-emerald-900'
      : type === 'error'
        ? 'border-rose-200 bg-rose-50 text-rose-900'
        : 'border-slate-200 bg-slate-50 text-slate-900';
    toast.className = 'flex items-start gap-3 rounded-lg border px-4 py-3 text-sm shadow ' + typeClass;
    toast.innerHTML = '<div class="mt-0.5 h-2 w-2 shrink-0 rounded-full bg-current opacity-60"></div>' +
      '<div class="flex-1">' + message + '</div>';
    container.appendChild(toast);
    window.setTimeout(function () {
      toast.remove();
    }, 4000);
  }

  render() {
    return html`<div id="vt-notification-container" class="fixed right-4 top-20 z-[3000] flex w-full max-w-sm flex-col gap-3"></div>`;
  }
}
