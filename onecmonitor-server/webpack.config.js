const path = require('path');
const webpack = require('webpack');


module.exports = {
    entry: {
        script: [
            './Scripts/index.ts'
        ]
    },
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
                use: ['style-loader', 'css-loader']
            },
        ],
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
    },
    output: {
        path: path.resolve(__dirname, 'wwwroot'),
        filename: 'dist/app.js',
        library: {
            
        }
    },
};