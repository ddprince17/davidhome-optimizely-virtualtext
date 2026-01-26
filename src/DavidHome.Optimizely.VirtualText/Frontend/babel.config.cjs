module.exports = {
  presets: [
    [
      '@babel/preset-env',
      {
        targets: 'defaults'
      }
    ],
    '@babel/preset-typescript'
  ],
  plugins: [
    ['@babel/plugin-proposal-decorators', { version: '2023-05' }],
    ['@babel/plugin-proposal-class-properties', { loose: false }],
    '@babel/plugin-transform-class-static-block'
  ],
  sourceType: 'unambiguous'
};
