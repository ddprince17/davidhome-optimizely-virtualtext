const path = require('path');
const MonacoWebpackPlugin = require('monaco-editor-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const CssMinimizerPlugin = require('css-minimizer-webpack-plugin');

module.exports = {
  mode: 'production',
  entry: {
    'virtualtext-app': path.resolve(__dirname, 'src/index.ts')
  },
  output: {
    path: path.resolve(__dirname, '..', 'ClientResources'),
    filename: '[name].js',
    chunkFilename: '[name].chunk.js',
    clean: true,
    globalObject: 'self'
  },
  resolve: {
    extensions: ['.ts', '.js']
  },
  target: 'web',
  optimization: {
    minimize: true,
    minimizer: ['...', new CssMinimizerPlugin()]
  },
  module: {
    rules: [
      {
        test: /\.[jt]s$/,
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
        use: [MiniCssExtractPlugin.loader, 'css-loader', 'postcss-loader']
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
