import argparse
from lxml import etree
import six
from google.cloud import translate_v2
import re
import os
import time
import os.path
import glob


def namespace(element):
    m = re.match(r'\{(.*)\}', element.tag)
    return m.group(1) if m else ''


def translate(source_file, language, language_code, subdir, service_account_file, allow_replace):
    translate_client = translate_v2.Client.from_service_account_json(service_account_file)

    tree = etree.parse(source_file)
    root = tree.getroot()
    ns_map = {'': namespace(root)}
    tags = root.find('tags', ns_map)
    tag = tags.find('tag', ns_map)
    tag.set('language', language)

    if not subdir:
        subdir = language_code.upper()

    filebase, ext = os.path.splitext(os.path.basename(source_file))
    target_file = os.path.join(os.path.dirname(source_file), subdir, f'{filebase}-{subdir}{ext}')

    if not allow_replace and os.path.isfile(target_file):
        print(f'Target file already exists, skipping {source_file}')
        return

    def non_empty_tag(s):
        tag = s.get('text')
        return tag and tag.strip()

    tags = list(filter(non_empty_tag, root.find('strings', ns_map).findall('string', ns_map)))
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
            s = s.replace(f'{{{idx}}}', '{' + m + '}').replace('\u00A0', '')
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
    CHUNK_SIZE = 10
    for idx, c in enumerate(chunks(translatable_strings, CHUNK_SIZE)):
        print(f'{int(CHUNK_SIZE * idx * 100 / len(translatable_strings))}%..', end='')
        tchunk = translate_client.translate(c, source_language='en', target_language=language_code)
        ct = [t["translatedText"] for t in tchunk]
        translations.extend(ct)
    print(f'100%')
    # try:
    #     translations = translate_client.translate(translatable_strings, source_language='en', target_language=language_code)
    #     # CHUNK_SIZE = 50
    #     # for idx, c in enumerate(chunks(translatable_strings, CHUNK_SIZE)):
    #     #     print(f'{int(CHUNK_SIZE * idx * 100 / len(translatable_strings))}%..', end='')
    #     #     ct = translator.translate(c, src='en', dest=language_code)
    #     #     if type(ct) == list:
    #     #         translations.extend(ct)
    #     #     elif ct:
    #     #         translations.append(ct)
    #     #     time.sleep(3)
    #     # print(f'100%')
    # except KeyboardInterrupt:
    #     raise
    # except Exception as e:
    #     print('  Error occurred while translating, try again: ', e)
    #     return

    for tag, e, t in zip(tags, string_escapes, translations):
        tag.set('text', unesc(t, e))

    os.makedirs(os.path.dirname(target_file), exist_ok=True)
    etree.ElementTree(root).write(target_file, encoding="utf-8", xml_declaration=True, pretty_print=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Translate Bannerlord strings xml files')
    parser.add_argument('glob_patterns', metavar='glob', type=str, nargs='+',
                        help='globs (wild card patterns) describing what files to translate, '
                             'output file is determined automatically')
    parser.add_argument('--account', dest='service_account_file', action='store', help='Path to Google Service Account Credentials json file', required=True)
    parser.add_argument('--lang', dest='lang', action='store', help='Bannerlord language name',
                        required=True)
    parser.add_argument('--replace', dest='replace', action='store_true', help='Allow replacing of existing translation files (WARNING: overwrites any manual changes in the target file)')
    parser.add_argument('--lang-code', dest='langcode', action='store', help='international language code for translation',
                        required=True)
    parser.add_argument('--subdir-override', dest='subdiroverride', action='store', help='Subdirectory override (usually it is uppercased lang-code)')
    args = parser.parse_args()

    expanded_files = [glob.glob(g, recursive=True) for g in args.glob_patterns]
    flattened_files = [f for files in expanded_files for f in files]
    unique_files = list(set(flattened_files))
    if len(unique_files) == 0:
        print('No files found matching the provided globs!')
    else:
        for f in unique_files:
            translate(f, args.lang, args.langcode, args.subdiroverride, args.service_account_file, args.replace)
