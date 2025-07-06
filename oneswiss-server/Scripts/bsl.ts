import * as monaco from 'monaco-editor';
import {languages} from "monaco-editor";
import IMonarchLanguage = languages.IMonarchLanguage;

const monarchBslLangDef: IMonarchLanguage =
    {
        "defaultToken": "",
        "ignoreCase": true,
        "keywords": [
            "процедура", "конецпроцедуры", "функция", "конецфункции",
            "если", "тогда", "иначеесли", "иначе", "конецесли",
            "для", "каждого", "из", "цикл", "конеццикла",
            "пока", "попытка", "исключение", "конецпопытки",
            "возврат", "продолжить", "прервать", "новый",
            "истина", "ложь", "неопределено", "null",
            "не", "и", "или", "экспорт", "перем", "знач", "по", "тип",
            "перейти", "вызватьисключение", "вернуть", "асинх", "ждать",
            "procedure", "endprocedure", "function", "endfunction",
            "if", "then", "elsif", "else", "endif", "for", "each", "in",
            "loop", "endloop", "while", "try", "except", "endtry",
            "return", "continue", "break", "new", "true", "false",
            "undefined", "not", "and", "or", "export", "var", "val",
            "by", "type", "goto", "raise",
            "async", "await"
        ],
        "preprocessorKeywords": [
            "#если", "#иначеесли", "#иначе", "#конецесли",
            "#область", "#конецобласти", "#использовать",
            "#if", "#elseif", "#else", "#endif",
            "#region", "#endregion", "#use"
        ],
        "operators": [
            "=", "+", "-", "*", "/", "%", "<", ">", "<=", ">=", "<>", ":", "."
        ],
        "symbols": /[=><!~?:&|+\-*\/\^%]+/,
        "tokenizer": {
            "root": [
                [/(\/\/).*/, "comment"],
                [/(#\w+)/, {
                    cases: {
                        "@preprocessorKeywords": "preprocessor",  // Фиолетовый (как ключевые, но другой оттенок)
                        "@default": "invalid"
                    }
                }],
                [/[а-яА-Яa-zA-Z][а-яА-Яa-zA-Z0-9_]*/, {
                    cases: {
                        "@keywords": "keyword.red",  // Ключевые слова — красные
                        "@default": "identifier.blue"  // Идентификаторы — синие
                    }
                }],
                [/\d+\.\d+/, "number.float"],
                [/\d+/, "number"],
                [/[()\[\]\{\}]/, "delimiter.bracket"],
                [/[;,.]/, "delimiter"],
                [/"([^"\\]|\\.)*$/, "string.invalid"],
                [/"/, { token: "string.quote", bracket: "@open", next: "@string" }],
                [/'([^'\\]|\\.)*$/, "string.invalid"],
                [/'/, { token: "string.quote", bracket: "@open", next: "@string_single" }]
            ],
            "string": [
                [/[^\\"]+/, "string"],
                [/\\./, "string.escape"],
                [/"/, { token: "string.quote", bracket: "@close", next: "@pop" }]
            ],
            "string_single": [
                [/[^\\']+/, "string"],
                [/\\./, "string.escape"],
                [/'/, { token: "string.quote", bracket: "@close", next: "@pop" }]
            ]
        }
    }

export function registerBslInMonaco() {
    monaco.languages.register({ id: 'bsl' });
    monaco.languages.setMonarchTokensProvider('bsl', monarchBslLangDef);

    monaco.editor.defineTheme('1c-theme', {
        base: 'vs',
        inherit: true,
        rules: [
            { token: 'keyword.red', foreground: 'ff0000', fontStyle: 'bold' },
            { token: 'identifier.blue', foreground: '0000ff' },
            { token: 'preprocessor', foreground: '800080' },
            { token: 'comment', foreground: '008000' },
            { token: 'string', foreground: 'a31515' },
            { token: 'number', foreground: '098658' },
            { token: 'delimiter', foreground: '000000' }
        ],
        colors: {
            'editor.foreground': '#000000', // Общий цвет текста
            'editor.background': '#b3b3b3', // Фон редактора
            'editorLineNumber.foreground': '#474747' // Цвет номеров строк
        }
    });
}