from os import listdir, rename
from os.path import isdir

fol = listdir()
print(fol)

for i in fol:
    if not isdir(i): continue
    cur = listdir(i)
    print(cur)
    for t in cur:
        if t.startswith(i.split('.')[0]):
            print('renamed', f'{i}/{t}')
            if t.endswith('open.png'): rename(f'{i}/{t}', f'{i}/base.png')
            else: rename(f'{i}/{t}', f'{i}/closed.png')
