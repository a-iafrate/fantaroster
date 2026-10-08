import xml.etree.ElementTree as ET

def update_resx(file_path, key, value):
    tree = ET.parse(file_path)
    root = tree.getroot()
    found = False
    for data in root.findall('data'):
        if data.get('name') == key:
            value_elem = data.find('value')
            if value_elem is not None:
                value_elem.text = value
            found = True
            break
    if not found:
        new_data = ET.SubElement(root, 'data', {'name': key, 'xml:space': 'preserve'})
        new_value = ET.SubElement(new_data, 'value')
        new_value.text = value

    ET.indent(tree, space="  ", level=0)
    tree.write(file_path, encoding='utf-8', xml_declaration=True)

# Update English
update_resx('src/Roster.Web/Resources/SharedResource.resx', 'CreateGameCaptainLabel', 'Enable captain')
update_resx('src/Roster.Web/Resources/SharedResource.resx', 'CreateGameCaptainMultiplierLabel', 'Captain multiplier')
update_resx('src/Roster.Web/Resources/SharedResource.resx', 'CreateGameError', 'Error: {0}')

# Update Italian
update_resx('src/Roster.Web/Resources/SharedResource.it.resx', 'CreateGameCaptainLabel', 'Abilita capitano')
update_resx('src/Roster.Web/Resources/SharedResource.it.resx', 'CreateGameCaptainMultiplierLabel', 'Moltiplicatore capitano')
update_resx('src/Roster.Web/Resources/SharedResource.it.resx', 'CreateGameError', 'Errore: {0}')

