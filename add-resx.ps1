param([string]$FilePath, [string]$Content)

$xml = [xml](Get-Content $FilePath)
$root = $xml.DocumentElement

$nodes = [xml]($Content)
foreach ($node in $nodes.root.ChildNodes) {
    if ($node -is [System.Xml.XmlElement]) {
        $importNode = $xml.ImportNode($node, $true)
        $root.AppendChild($importNode) > $null
    }
}

$xml.Save($FilePath)
