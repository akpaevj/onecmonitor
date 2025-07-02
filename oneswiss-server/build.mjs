import * as esbuild from 'esbuild';

await esbuild.build({
    entryPoints: ['./Scripts/index.ts'],
    bundle: true,
    outfile: './wwwroot/bundle.js',
    loader: {
        ".woff" : 'file',
        ".woff2" : 'file',
        ".ttf" : 'file'
    },
    minify: true,
    globalName: 'OM',
    sourcemap: 'inline'
})