const path = require('path');
const MonacoWebpackPlugin = require('monaco-editor-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const CssMinimizerPlugin = require('css-minimizer-webpack-plugin');

module.exports = {
  mode: 'production',
  entry: {
    'monaco-editor': path.resolve(__dirname, 'src/monaco.bundle.js'),
    'virtualtext-app': {
      import: path.resolve(__dirname, 'src/index.js'),
      dependOn: 'monaco-editor'
    }
  },
  output: {
    path: path.resolve(__dirname, '..', 'ClientResources'),
    filename: '[name].js',
    chunkFilename: '[name].chunk.js',
    clean: true,
    globalObject: 'self'
  },
  target: 'web',
  optimization: {
    minimize: true,
    minimizer: ['...', new CssMinimizerPlugin()]
  },
  module: {
    parser: {
      javascript: {
        dynamicImportMode: 'eager'
      }
    },
    rules: [
      {
        test: /\.js$/,
        include: [
          path.resolve(__dirname, 'src'),
          path.resolve(__dirname, 'node_modules/monaco-editor')
        ],
        type: 'javascript/auto',
        parser: {
          sourceType: 'module'
        },
        use: {
          loader: 'babel-loader'
        }
      },
      {
        test: /\.css$/,
        use: [MiniCssExtractPlugin.loader, 'css-loader']
      },
      {
        test: /\.ttf$/,
        type: 'asset/inline'
      }
    ]
  },
  plugins: [
    new MiniCssExtractPlugin({
      filename: '[name].css'
    }),
    new MonacoWebpackPlugin({
      languages: [],
      filename: 'monaco-editor.[name].worker.js'
    })
  ]
};
