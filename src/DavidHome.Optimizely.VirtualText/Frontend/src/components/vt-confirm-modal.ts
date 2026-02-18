import { LitElement, html } from 'lit';
import { customElement } from 'lit/decorators.js';

@customElement('vt-confirm-modal')
export class VtConfirmModal extends LitElement {
  createRenderRoot() {
    return this;
  }

  open(message: string, confirmText: string, cancelText: string) {
    const modal = this.querySelector<HTMLDivElement>('#vt-confirm-modal');
    const messageEl = this.querySelector<HTMLParagraphElement>('#vt-confirm-message');
    const confirmButton = this.querySelector<HTMLButtonElement>('#vt-confirm-accept');
    const cancelButton = this.querySelector<HTMLButtonElement>('#vt-confirm-cancel');
    if (!modal || !messageEl || !confirmButton || !cancelButton) {
      return Promise.resolve(false);
    }

    messageEl.textContent = message;
    confirmButton.textContent = confirmText;
    cancelButton.textContent = cancelText;
    modal.removeAttribute('inert');
    modal.classList.remove('hidden');
    modal.hidden = false;
    modal.setAttribute('aria-hidden', 'false');

    return new Promise<boolean>((resolve) => {
      const cleanup = (value: boolean) => {
        confirmButton.removeEventListener('click', onConfirm);
        cancelButton.removeEventListener('click', onCancel);
        modal.removeEventListener('click', onBackdrop);
        this.close();
        resolve(value);
      };

      const onConfirm = () => cleanup(true);
      const onCancel = () => cleanup(false);
      const onBackdrop = (event: Event) => {
        if (event.target instanceof HTMLElement && event.target.classList.contains('vt-confirm-backdrop')) {
          cleanup(false);
        }
      };

      confirmButton.addEventListener('click', onConfirm);
      cancelButton.addEventListener('click', onCancel);
      modal.addEventListener('click', onBackdrop);
    });
  }

  close() {
    const modal = this.querySelector<HTMLDivElement>('#vt-confirm-modal');
    if (!modal) {
      return;
    }
    if (modal.contains(document.activeElement)) {
      (document.activeElement as HTMLElement).blur();
    }
    modal.setAttribute('inert', '');
    modal.classList.add('hidden');
    modal.hidden = true;
    modal.setAttribute('aria-hidden', 'true');
  }

  render() {
    return html`
      <div id="vt-confirm-modal" class="fixed inset-0 z-[2200] hidden" aria-hidden="true" hidden>
        <div class="vt-confirm-backdrop fixed inset-0 bg-slate-900/60"></div>
        <div class="fixed inset-0 flex items-center justify-center p-4">
          <div class="w-full max-w-md rounded-xl bg-white shadow-xl">
            <div class="p-6">
              <h2 class="text-lg font-semibold text-slate-900">Please confirm</h2>
              <p id="vt-confirm-message" class="mt-2 text-sm text-slate-600"></p>
            </div>
            <div class="flex items-center justify-end gap-3 border-t border-slate-200 px-6 py-4">
              <button type="button" id="vt-confirm-cancel" class="rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50">Cancel</button>
              <button type="button" id="vt-confirm-accept" class="rounded-md bg-slate-900 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-800">Confirm</button>
            </div>
          </div>
        </div>
      </div>
    `;
  }
}
