const path = require('path');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const CssMinimizerPlugin = require('css-minimizer-webpack-plugin');

module.exports = {
  mode: 'production',
  entry: {
    'robotstxt-app': path.resolve(__dirname, 'src/index-app.ts')
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
        include: [path.resolve(__dirname, 'src')],
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
      }
    ]
  },
  plugins: [
    new MiniCssExtractPlugin({
      filename: '[name].css'
    })
  ]
};
