const path = require('path');
const webpack = require('webpack');

const { CleanWebpackPlugin } = require("clean-webpack-plugin");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");

module.exports = {
    entry: {
        script: [
            './Scripts/site.ts'
        ]
    },
    mode: 'development',
    devtool: 'inline-source-map',
    optimization: {
        minimize: false,
        usedExports: false
    },
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                loader: 'ts-loader',
                options: {
                    transpileOnly: true
                }
            },
            {
                test: /\.css$/i,
                use: [
                    MiniCssExtractPlugin.loader,
                    "css-loader"
                ]
            },
        ],
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
    },
    output: {
        library: {
            name: "OM",
            type: "var"
        },
        filename: 'dist/app.js',
        path: path.resolve(__dirname, 'wwwroot'),
    },
    plugins: [
        new CleanWebpackPlugin({
            cleanOnceBeforeBuildPatterns: ["dist"]
        }),
        new MiniCssExtractPlugin({
            filename: "css/site.css",
        }),
        new webpack.ContextReplacementPlugin(/moment[/\\]locale$/, /en|ru/)
    ],
};