import xml.etree.ElementTree as ET

def create_resx(file_path, entries):
    root = ET.Element('root')
    
    # Add schema and resheaders
    schema_xml = """
  <xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
    <xsd:element name="root" msdata:IsDataSet="true">
      <xsd:complexType>
        <xsd:choice maxOccurs="unbounded">
          <xsd:element name="data">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
                <xsd:element name="comment" type="xsd:string" minOccurs="0" msdata:Ordinal="2" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" />
              <xsd:attribute name="type" type="xsd:string" />
              <xsd:attribute name="mimetype" type="xsd:string" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="resheader">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
"""
    root.append(ET.fromstring(schema_xml))
    
    resheaders = [
        ('resmimetype', 'text/microsoft-resx'),
        ('version', '2.0'),
        ('reader', 'System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'),
        ('writer', 'System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089')
    ]
    for name, value in resheaders:
        rh = ET.SubElement(root, 'resheader')
        rh.set('name', name)
        v = ET.SubElement(rh, 'value')
        v.text = value

    for key, val in entries.items():
        data = ET.SubElement(root, 'data')
        data.set('name', key)
        data.set('xml:space', 'preserve')
        value = ET.SubElement(data, 'value')
        value.text = val
        
    tree = ET.ElementTree(root)
    tree.write(file_path, encoding='utf-8', xml_declaration=True)

common_entries = {
    'DashboardTitle': 'My Games',
    'NewGame': 'New game',
    'FilterAll': 'All',
    'FilterDrafts': 'Drafts',
    'FilterLive': 'Live',
    'FilterArchived': 'Archived',
    'ManageLive': 'Manage live',
    'OpenGame': 'Open',
    'DuplicateTitle': 'Duplicate an edition',
    'DuplicateDesc': 'Start from rules and elements of a past game.',
    'NavTemplates': 'Templates',
    'NavBilling': 'Plan and billing',
    'NavHelp': 'Help',
    'PlanFree': 'Free Plan',
    'PlanFreeDesc': '1 live game, up to 50 players.',
    'PlanUpgrade': 'Upgrade to Event',
    'CreateGameTitle': 'New game',
    'Loading': 'Loading...'
}

en_entries = dict(common_entries, **{
    'CreateGameStep1Title': 'What is the game about?',
    'CreateGameStep2Title': 'Game settings',
    'CreateGameStep3Title': 'Where do elements come from?',
    'CreateGameStep4Title': 'Check who is in play',
    'CreateGameStep5Title': 'Rules and points',
    'CreateGameStep6Title': 'All set',
    'CreateGameNameLabel': 'Game name',
    'CreateGameLanguageLabel': 'Language',
    'CreateGameLineupSizeLabel': 'Elements per lineup',
    'CreateGameCaptainLabel': 'Enable captain (double points)',
    'BtnBack': 'Back',
    'BtnContinue': 'Continue',
    'BtnPublish': 'Publish Game',
    'CreateGamePublishDesc': 'Create the game to start.',
    'CreateGameSourceDesc': 'You can change the source until publication.',
    'CreateGameRulesDesc': 'These are the base rules. You can add more later.',
    
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
})

it_entries = {
    'DashboardTitle': 'Le mie partite',
    'NewGame': 'Nuova partita',
    'FilterAll': 'Tutte',
    'FilterDrafts': 'Bozze',
    'FilterLive': 'Live',
    'FilterArchived': 'Archiviate',
    'ManageLive': 'Gestisci live',
    'OpenGame': 'Apri',
    'DuplicateTitle': 'Duplica un\'edizione',
    'DuplicateDesc': 'Riparti da regole ed elementi di una partita passata.',
    'NavTemplates': 'Modelli',
    'NavBilling': 'Piano e fatture',
    'NavHelp': 'Aiuto',
    'PlanFree': 'Piano Gratis',
    'PlanFreeDesc': '1 partita live, fino a 50 giocatori.',
    'PlanUpgrade': 'Passa a Evento',
    'CreateGameTitle': 'Nuova partita',
    'Loading': 'Caricamento...',

    'CreateGameStep1Title': 'Di cosa è il fanta?',
    'CreateGameStep2Title': 'Impostazioni partita',
    'CreateGameStep3Title': 'Da dove prendiamo gli elementi?',
    'CreateGameStep4Title': 'Controlla chi entra in gioco',
    'CreateGameStep5Title': 'Regole e punteggi',
    'CreateGameStep6Title': 'Tutto pronto',
    'CreateGameNameLabel': 'Nome della partita',
    'CreateGameLanguageLabel': 'Lingua',
    'CreateGameLineupSizeLabel': 'Elementi per rosa',
    'CreateGameCaptainLabel': 'Abilita capitano (i suoi punti valgono doppio)',
    'BtnBack': 'Indietro',
    'BtnContinue': 'Continua',
    'BtnPublish': 'Pubblica Partita',
    'CreateGamePublishDesc': 'Crea la partita per iniziare.',
    'CreateGameSourceDesc': 'Puoi cambiare sorgente fino alla pubblicazione.',
    'CreateGameRulesDesc': 'Queste sono le regole di base. Potrai aggiungerne altre in seguito.',
    
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

create_resx('src/Roster.Web/Resources/SharedResource.resx', en_entries)
create_resx('src/Roster.Web/Resources/SharedResource.it.resx', it_entries)

