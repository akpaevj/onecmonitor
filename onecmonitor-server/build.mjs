import * as esbuild from 'esbuild';
import * as path from "node:path";

await esbuild.build({
    entryPoints: ['./Scripts/index.ts'],
    bundle: true,
    outfile: './wwwroot/bundle.js',
    loader: {
        ".woff" : 'file',
        ".woff2" : 'file'
    },
    minify: true,
    globalName: 'OM',
    sourcemap: 'inline'
})