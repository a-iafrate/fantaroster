import os

with open("src/Roster.Web/Components/Pages/Organizer/CreateGame.razor", "r", encoding="utf-8") as f:
    content = f.read()

# Fix culture in pack name and desc
content = content.replace(
    'pack.Name.TryGetValue("en", out var n)',
    'pack.Name.TryGetValue(System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, out var n) || pack.Name.TryGetValue("en", out n)'
)
content = content.replace(
    'pack.Description.TryGetValue("en", out var d)',
    'pack.Description.TryGetValue(System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, out var d) || pack.Description.TryGetValue("en", out d)'
)

with open("src/Roster.Web/Components/Pages/Organizer/CreateGame.razor", "w", encoding="utf-8") as f:
    f.write(content)

