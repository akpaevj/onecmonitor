const path = require('path');
const { CleanWebpackPlugin } = require("clean-webpack-plugin");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const webpack = require('webpack');

module.exports = {
    entry: {
        script: ['./Scripts/site.ts'],
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
                include: /Scripts/,
                exclude: /node_modules/,
                options: {
                    transpileOnly: true
                }
            },
            {
                test: /\.css$/i,
                use: [MiniCssExtractPlugin.loader, "css-loader"],
                include: path.resolve(__dirname, 'Styles/site.css'),
                exclude: /node_modules/,
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