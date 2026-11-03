"""Run with python3 -m unittest discover -s tests (install requirements.txt first)."""
import copy
import json
import re
from pathlib import Path
import unittest
from jsonschema import Draft4Validator, FormatChecker

ROOT = Path(__file__).resolve().parents[1]

def read(path):
    return json.loads((ROOT / path).read_text(encoding="utf-8"))

class Schemas(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        checker = FormatChecker()
        assert not checker.conforms('AllCards.json', 'uri'), 'URI format checker must be active'
        assert not checker.conforms('a bad reference', 'uri-reference'), 'URI-reference checker must be active'
        cls.game = Draft4Validator(read('schema/cgs.json'), format_checker=checker)
        cls.catalog = Draft4Validator(read('schema/GameLocalization.json'), format_checker=checker)
        cls.legacy = Draft4Validator(read('FinolDigital.Cgs.Json.Tests/Fixtures/legacy-cgs.schema.json'), format_checker=checker)
        cls.example = read('examples/localization/cgs.json')

    def test_legacy_contract_unchanged(self):
        old = self.legacy.schema
        new = copy.deepcopy(self.game.schema)
        del new['properties']['localization']
        new['definitions'] = {k: v for k, v in new['definitions'].items() if k in old['definitions']}
        self.assertEqual(old, new)

    def test_examples(self):
        self.game.validate(self.example)
        self.catalog.validate(read('examples/localization/locales/es.json'))
        base = copy.deepcopy(self.example)
        del base['localization']
        self.game.validate(base)
        self.legacy.validate(base)
        self.assertFalse(self.legacy.is_valid(self.example))
        base['allCardsUrl'] = 'AllCards.json'
        self.assertFalse(self.legacy.is_valid(base))
        self.assertFalse(self.game.is_valid(base))

    def test_sources(self):
        for source in ({'inline': {}}, {'url': 'locales/es.json'}, {'url': 'https://example.invalid/es.json'}):
            self.game.validate({'name':'Game', 'localization':{'defaultLocale':'ja','locales':{'es':source}}})
        for source in ({}, {'inline':{},'url':'es.json'}, {'inline':None}, {'url':''}, {'url':'bad url'}):
            self.assertFalse(self.game.is_valid({'name':'Game','localization':{'defaultLocale':'ja','locales':{'es':source}}}))
        self.assertFalse(self.game.is_valid({'name':'Game','localization':{}}))
        self.game.validate({'name':'Game','localization':{'defaultLocale':'ja'}})

    def test_catalog_leaf_types(self):
        self.catalog.validate({})
        self.catalog.validate({'cards':{'AbC':{'name':'','properties':{'Rules':'text'}}},'properties':{'ready':{'booleanValues':{'true':'Sí'}}}})
        for value in (None, 42, False, [], {}):
            self.assertFalse(self.catalog.is_valid({'cards': {'AbC': {'name':value}}}))
        for value in ('', 'bad path', 'https://example.invalid/bad path'):
            self.assertFalse(self.catalog.is_valid({'assets': {'cardImages': {'AbC': value}}}))
        self.catalog.validate({'assets': {'cardImages': {'AbC': '../images/card.png'}}})
        self.assertFalse(self.catalog.is_valid({'properties': {'ready': {'booleanValues': {'other':'x'}}}}))

    def test_documented_json(self):
        for example in re.findall(r'```json\n(.*?)\n```', (ROOT / 'docs/localization.md').read_text(encoding="utf-8"), re.S):
            self.game.validate(json.loads(example))

    def test_base_language_fixtures(self):
        for locale in ('en','es','ja'):
            prefix = 'FinolDigital.Cgs.Json.Tests/Fixtures/' + locale + '/'
            base = read(prefix + 'cgs.json')
            self.legacy.validate(base)
            self.game.validate(base)
            self.game.validate(read(prefix + 'localized.json'))
