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


def translate(source_file, language, language_code, subdir, service_account_file, replace,
              update_original, update_changed):
    translate_client = translate_v2.Client.from_service_account_json(service_account_file)

    source_tree = etree.parse(source_file)
    source_root = source_tree.getroot()

    ns_map = {'': namespace(source_root)}

    if not subdir:
        subdir = language_code.upper()

    file_base, ext = os.path.splitext(os.path.basename(source_file))
    target_file = os.path.join(os.path.dirname(source_file), subdir, f'{file_base}-{subdir}{ext}')

    if not (replace or update_original or update_changed) and os.path.isfile(target_file):
        print(f'Target file already exists, skipping {source_file}')
        return

    language_tag = source_root.find('tags', ns_map).find('tag', ns_map)
    language_tag.set('language', language)

    def non_empty_tag(s):
        text_attr = s.get('text')
        return text_attr and text_attr.strip()

    source_string_tags = list(filter(non_empty_tag, source_root.find('strings', ns_map).findall('string', ns_map)))
    strings = [tag.get('text') for tag in source_string_tags]

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
    INDENT = '  > '
    print(INDENT, end='')
    for idx, c in enumerate(chunks(translatable_strings, CHUNK_SIZE)):
        print(f'{int(CHUNK_SIZE * idx * 100 / len(translatable_strings))}%..', end='')
        tchunk = translate_client.translate(c, source_language='en', target_language=language_code)
        ct = [t["translatedText"] for t in tchunk]
        translations.extend(ct)
    print(f'100%')

    for tag, e, t, o in zip(source_string_tags, string_escapes, translations, strings):
        tag.set('text', unesc(t, e))
        tag.set('original', o)

    # If the file exists, and we didn't specify entirely replacing it, we will merge existing content, and warn on
    # changed content
    if not replace and os.path.isfile(target_file):
        target_tree = etree.parse(target_file)
        target_root = target_tree.getroot()
        target_strings_root = target_root.find('strings', ns_map)
        if target_strings_root is None:
            # Just save the translated source directly and return
            print(INDENT + f'Target file has no "strings" element, replacing entirely')
            etree.ElementTree(source_root).write(target_file, encoding="utf-8", xml_declaration=True, pretty_print=True)
            return
        # Do merging by id
        target_string_tags = list(filter(non_empty_tag, target_strings_root.findall('string', ns_map)))
        for idx, source_string_tag in enumerate(source_string_tags):
            source_id = source_string_tag.get('id')
            source_original = source_string_tag.get('original')
            source_text = source_string_tag.get('text')
            try:
                target_string_tag = next(t for t in target_string_tags if t.get('id') == source_id)
                target_original = target_string_tag.get('original')
                if target_original != source_original and (update_original or update_changed) or not target_original:
                    target_string_tag.set('original', source_original)
                    print(INDENT + f'Updated {source_id} original attribute')
                    print(INDENT + f'  from "{target_original}"')
                    print(INDENT + f'  to "{source_original}"')
                target_text = target_string_tag.get('text')
                if update_changed and target_text != source_text:
                    target_string_tag.set('text', source_text)
                    print(INDENT + f'Updated {source_id} text attribute')
                    print(INDENT + f'  from "{target_text}"')
                    print(INDENT + f'  to "{source_text}"')
            except StopIteration:
                target_string_tag = etree.SubElement(target_strings_root, 'string')
                target_string_tag.set('id', source_id)
                target_string_tag.set('text', source_text)
                target_string_tag.set('original', source_original)
                print(INDENT + f'Added {source_id}')
                print(INDENT + f'  original "{source_original}"')
                print(INDENT + f'  text "{source_text}"')
        # Fix indents
        etree.indent(target_root, space="    ")
        etree.ElementTree(target_root).write(target_file, encoding="utf-8", xml_declaration=True, pretty_print=True)
    else:
        os.makedirs(os.path.dirname(target_file), exist_ok=True)
        etree.ElementTree(source_root).write(target_file, encoding="utf-8", xml_declaration=True, pretty_print=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Translate Bannerlord strings xml files')
    parser.add_argument('glob_patterns', metavar='glob', type=str, nargs='+',
                        help='globs (wild card patterns) describing what files to translate, '
                             'output file is determined automatically')
    parser.add_argument('--account', dest='service_account_file', action='store',
                        help='Path to Google Service Account Credentials json file', required=True)
    parser.add_argument('--lang', dest='lang', action='store',
                        help='Bannerlord language name', required=True)
    parser.add_argument('--replace', dest='replace', action='store_true',
                        help='Entirely replace target file. WARNING: overwrites any manual changes in the target file!')
    parser.add_argument('--update-original-tag', dest='update_original_tag', action='store_true',
                        help='Update the "original" tag in the target file, if it exists')
    parser.add_argument('--update-changed', dest='update_changed', action='store_true',
                        help='Update the translation if the original text has changed, implies update-original-tag')
    parser.add_argument('--lang-code', dest='langcode', action='store',
                        help='international language code for translation', required=True)
    parser.add_argument('--subdir-override', dest='subdiroverride', action='store',
                        help='Subdirectory override (usually it is uppercased lang-code)')
    args = parser.parse_args()

    expanded_files = [glob.glob(g, recursive=True) for g in args.glob_patterns]
    flattened_files = [f for files in expanded_files for f in files]
    unique_files = list(set(flattened_files))
    if len(unique_files) == 0:
        print('No files found matching the provided globs!')
    else:
        for f in unique_files:
            translate(f, args.lang, args.langcode, args.subdiroverride, args.service_account_file, args.replace,
                      args.update_original_tag, args.update_changed)
