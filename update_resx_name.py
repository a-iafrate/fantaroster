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

add_entries('src/Roster.Web/Resources/SharedResource.resx', {'NameRequired': 'Name is required'})
add_entries('src/Roster.Web/Resources/SharedResource.it.resx', {'NameRequired': 'Il nome della partita è obbligatorio'})

