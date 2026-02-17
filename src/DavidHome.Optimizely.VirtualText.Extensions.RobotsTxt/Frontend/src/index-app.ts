import './app.css';

document.addEventListener('DOMContentLoaded', () => {
  const containers = document.querySelectorAll<HTMLElement>('.rt-directive-combobox');

  const closeAllMenus = () => {
    document.querySelectorAll<HTMLElement>('.rt-directive-menu').forEach((menu) => {
      menu.classList.add('hidden');
    });
  };

  containers.forEach((container) => {
    const input = container.querySelector<HTMLInputElement>('.rt-directive-input');
    const toggle = container.querySelector<HTMLButtonElement>('.rt-directive-toggle');
    const menu = container.querySelector<HTMLElement>('.rt-directive-menu');
    const options = container.querySelectorAll<HTMLButtonElement>('.rt-directive-option');

    if (!input || !toggle || !menu) {
      return;
    }

    toggle.addEventListener('click', (event) => {
      event.preventDefault();
      const isOpen = !menu.classList.contains('hidden');
      closeAllMenus();
      if (!isOpen) {
        menu.classList.remove('hidden');
      }
    });

    options.forEach((option) => {
      option.addEventListener('click', (event) => {
        event.preventDefault();
        input.value = option.dataset.value ?? '';
        menu.classList.add('hidden');
        input.focus();
      });
    });
  });

  document.addEventListener('click', (event) => {
    const target = event.target as HTMLElement;
    if (!target.closest('.rt-directive-combobox')) {
      closeAllMenus();
    }
  });
});
