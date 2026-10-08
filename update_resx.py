import xml.etree.ElementTree as ET

def add_entries(file_path, entries):
    tree = ET.parse(file_path)
    root = tree.getroot()
    
    existing_keys = {data.get('name') for data in root.findall('data')}
    
    for key, val in entries.items():
        if key not in existing_keys:
            data = ET.SubElement(root, 'data')
            data.set('name', key)
            data.set('xml:space', 'preserve')
            value = ET.SubElement(data, 'value')
            value.text = val
        else:
            for data in root.findall('data'):
                if data.get('name') == key:
                    data.find('value').text = val
                    
    tree.write(file_path, encoding='utf-8', xml_declaration=True)

en_entries = {
    'StepScope': 'Scope',
    'StepSettings': 'Settings',
    'StepSource': 'Source',
    'StepElements': 'Elements',
    'StepRules': 'Rules',
    'StepPublish': 'Publish',
    
    'SourceSessionize': 'Sessionize',
    'SourceSessionizeDesc': 'Speakers and sessions from your event API.',
    'SourceCSV': 'CSV',
    'SourceCSVDesc': 'One row per element (not fully supported yet).',
    'SourceNone': 'None',
    'SourceNoneDesc': 'You will add elements manually later.',
    
    'SessionizeApiId': 'Sessionize API ID',
    'SessionizePlaceholder': 'e.g. k3x9vq2m',
    'SessionizeFound': 'Found {0} elements',
    'SessionizeError': 'Import error: {0}',
    'SessionizeCsvError': 'CSV preview is not implemented in this mockup.',
    
    'PreviewElementsCount': 'Found {0} elements. We will send a consent request later.',
    'ColName': 'Name',
    'ColSubtitle': 'Subtitle',
    'ColGroup': 'Group',
    
    'ImportAndContinue': 'Import and continue'
}

it_entries = {
    'StepScope': 'Ambito',
    'StepSettings': 'Impostazioni',
    'StepSource': 'Sorgente',
    'StepElements': 'Elementi',
    'StepRules': 'Regole',
    'StepPublish': 'Pubblica',
    
    'SourceSessionize': 'Sessionize',
    'SourceSessionizeDesc': 'Speaker e sessioni dall\'API del tuo evento.',
    'SourceCSV': 'CSV',
    'SourceCSVDesc': 'Una riga per elemento (non ancora supportato nel wizard interamente).',
    'SourceNone': 'Nessuna',
    'SourceNoneDesc': 'Aggiungerai elementi a mano in seguito.',
    
    'SessionizeApiId': 'ID API Sessionize',
    'SessionizePlaceholder': 'e.g. k3x9vq2m',
    'SessionizeFound': 'Trovati {0} elementi',
    'SessionizeError': 'Errore durante l\'importazione: {0}',
    'SessionizeCsvError': 'Anteprima CSV non implementata in questo mockup.',
    
    'PreviewElementsCount': '{0} elementi trovati. Invieremo una richiesta di consenso in seguito.',
    'ColName': 'Nome',
    'ColSubtitle': 'Sottotitolo',
    'ColGroup': 'Gruppo',
    
    'ImportAndContinue': 'Importa e continua'
}

add_entries('src/Roster.Web/Resources/SharedResource.en.resx', en_entries)
add_entries('src/Roster.Web/Resources/SharedResource.it.resx', it_entries)
