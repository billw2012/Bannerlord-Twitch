import argparse
from lxml import etree
from pygoogletranslation import Translator
import re
import os
import sys
import os.path
import glob


def namespace(element):
    m = re.match(r'\{(.*)\}', element.tag)
    return m.group(1) if m else ''


def language_to_code(language):
    if language == "Türkçe":
        return "tr"
    elif language == "Deutsch":
        return "de"
    return "en"


def translate(source_file, language):
    translator = Translator()
    tree = etree.parse(source_file)
    root = tree.getroot()
    ns_map = {'': namespace(root)}
    tags = root.find('tags', ns_map)
    tag = tags.find('tag', ns_map)
    tag.set('language', language)

    language_code = language_to_code(language)

    target_file = os.path.join(os.path.dirname(source_file), language_code.upper(), os.path.basename(source_file))

    if os.path.isfile(target_file):
        print(f'Target file already exists, skipping {source_file}')
        return

    tags = root.find('strings', ns_map).findall('string', ns_map)
    strings = [tag.get('text') for tag in tags]

    if len(strings) == 0:
        print(f'No translatable strings found, skipping {source_file}')
        return

    regex = re.compile(r'\{(.*)\}')

    def esc(s):
        unique_matches = set(re.findall(regex, s))
        for idx, m in enumerate(unique_matches):
            s = s.replace('{' + m + '}', f'{{{idx}}}')
        return s, unique_matches

    def unesc(s, unique_matches):
        for idx, m in enumerate(unique_matches):
            s = s.replace(f'{{{idx}}}', '{' + m + '}')
        return s

    escaped_strings = [esc(s) for s in strings]
    translatable_strings = [s for s, _ in escaped_strings]
    string_escapes = [e for _, e in escaped_strings]
    # placeholders = [regex.sub() for s in strings]

    print(f'Translating {len(translatable_strings)} strings \n'
          f'  from: {source_file}\n'
          f'  to: {language}\n'
          f'  output: {target_file}')

    def chunks(lst, n):
        """Yield successive n-sized chunks from lst."""
        for i in range(0, len(lst), n):
            yield lst[i:i + n]

    translations = []

    print('  ', end='')
    try:
        CHUNK_SIZE = 10
        for idx, c in enumerate(chunks(translatable_strings, CHUNK_SIZE)):
            print(f'{int(CHUNK_SIZE * idx * 100 / len(translatable_strings))}%..', end='')
            ct = translator.translate(c, src='en', dest=language_code)
            translations.extend(ct)
        print(f'100%')
    except:
        print('\n  Error occurred while translating, try again: ', sys.exc_info()[0])
    for tag, e, t in zip(tags, string_escapes, translations):
        tag.set('text', unesc(t.text, e))

    os.makedirs(os.path.dirname(target_file), exist_ok=True)
    etree.ElementTree(root).write(target_file, encoding="utf-8", xml_declaration=True, pretty_print=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Translate Bannerlord strings xml files')
    parser.add_argument('glob_patterns', metavar='glob', type=str, nargs='+',
                        help='globs (wild card patterns) describing what files to translate, '
                             'output file is determined automatically')
    parser.add_argument('--lang', dest='lang', action='store', help='specify language (using Bannerlord naming)',
                        required=True)
    args = parser.parse_args()

    expanded_files = [glob.glob(g, recursive=True) for g in args.glob_patterns]
    flattened_files = [f for files in expanded_files for f in files]
    unique_files = list(set(flattened_files))
    if len(unique_files) == 0:
        print('No files found matching the provided globs!')
    else:
        for f in unique_files:
            translate(f, args.lang)
