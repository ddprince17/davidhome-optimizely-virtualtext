var monaco = window.VirtualTextMonaco && window.VirtualTextMonaco.monaco;

function resolveContainer(containerOrId) {
  if (typeof containerOrId === 'string') {
    return document.getElementById(containerOrId);
  }

  return containerOrId;
}

function createEditor(containerOrId, value, options) {
  var container = resolveContainer(containerOrId);
  if (!container) {
    throw new Error('Monaco container not found.');
  }

  if (!monaco) {
    throw new Error('Monaco not loaded. Load monaco-editor.js before virtualtext-app.js.');
  }

  return monaco.editor.create(
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

window.VirtualTextMonaco = {
  createEditor: createEditor,
  monaco: monaco
};
