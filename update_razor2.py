import os
import re

with open("src/Roster.Web/Components/Pages/Organizer/CreateGame.razor", "r", encoding="utf-8") as f:
    content = f.read()

# Fix EditForm in Step 2
content = content.replace(
    '<EditForm Model="_state" OnValidSubmit="NextStep" style="display:flex; flex-direction:column; flex:1;">',
    '<EditForm Model="_state" OnValidSubmit="NextStep">'
)
content = content.replace(
    '<div style="display:flex; flex-direction:column; gap:16px; flex:1;">',
    '<div style="display:flex; flex-direction:column; gap:16px">'
)

# Fix bottom navs
content = content.replace('class="cg-bottom-nav right"', 'class="cg-bottom-nav right cg-mt-auto"')
content = content.replace('class="cg-bottom-nav"', 'class="cg-bottom-nav cg-mt-auto"')

# But step 2 bottom nav shouldn't be auto, it should be 40.
content = content.replace(
    '<div class="cg-bottom-nav cg-mt-auto" style="margin-top:40px">',
    '<div class="cg-bottom-nav cg-mt-40">'
)

with open("src/Roster.Web/Components/Pages/Organizer/CreateGame.razor", "w", encoding="utf-8") as f:
    f.write(content)

