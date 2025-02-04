import {resolve as _resolve} from 'path';
import {ContextReplacementPlugin} from 'webpack';

import {CleanWebpackPlugin} from "clean-webpack-plugin";
import MiniCssExtractPlugin, {loader as _loader} from "mini-css-extract-plugin";

export const entry = {
    script: [
        './Scripts/site.ts'
    ]
};
export const devtool = 'inline-source-map';
export const optimization = {
    minimize: false,
    usedExports: false
};
export const module = {
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
                _loader,
                "css-loader"
            ]
        },
    ],
};
export const resolve = {
    extensions: ['.tsx', '.ts', '.js'],
};
export const output = {
    library: {
        name: "OM",
        type: "var"
    },
    filename: 'dist/app.js',
    path: _resolve(__dirname, 'wwwroot'),
};
export const plugins = [
    new CleanWebpackPlugin({
        cleanOnceBeforeBuildPatterns: ["dist"]
    }),
    new MiniCssExtractPlugin({
        filename: "css/site.css",
    }),
    new ContextReplacementPlugin(/moment[/\\]locale$/, /en|ru/)
];