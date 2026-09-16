import os
import shutil
import hashlib

def get_md5(filepath):
    h = hashlib.md5()
    with open(filepath, 'rb') as f:
        while chunk := f.read(8192):
            h.update(chunk)
    return h.hexdigest()

def ensure_bom(filepath):
    with open(filepath, 'rb') as f:
        data = f.read()
    if not data.startswith(b'\xef\xbb\xbf'):
        with open(filepath, 'wb') as f:
            f.write(b'\xef\xbb\xbf' + data)
        print(f"Added UTF-8 BOM to {filepath}")

# 1. Check & Ensure BOM on source views
views_to_sync = [
    r"d:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_DigitalSales.cshtml",
    r"d:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_DetailTracking.cshtml",
    r"d:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_TrackingLogsModal.cshtml",
    r"d:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_TrackingForm.cshtml",
    r"d:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_UnlockProgressModal.cshtml",
    r"d:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_ImportProgressModal.cshtml",
    r"d:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_StatusTimelineModal.cshtml",
    r"d:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_StatusTimelineDetailModal.cshtml",
    r"d:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_ImportTodoModal.cshtml",
    r"d:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\DigitalSalesDetail.js"
]
css_files_to_sync = [
    r"d:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_DetailTracking.css",
    r"d:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_StatusTimelineModal.css",
    r"d:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_StatusTimelineDetailModal.css",
    r"d:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_TrackingLogsModal.css"
]

for v in views_to_sync:
    ensure_bom(v)

# 2. Destinations
targets = [
    r"d:\SVN\crm\publish_source",
    r"d:\SVN\crm\CenIT.Solution.TOC.WebApp"
]

for t in targets:
    # Copy views & css
    dest_view_dir = os.path.join(t, r"Areas\Cate\Views\DigitalSales")
    os.makedirs(dest_view_dir, exist_ok=True)
    
    for v in views_to_sync:
        shutil.copy2(v, os.path.join(dest_view_dir, os.path.basename(v)))
        
    for c in css_files_to_sync:
        shutil.copy2(c, os.path.join(dest_view_dir, os.path.basename(c)))
    
    # Copy DLLs
    dest_bin = os.path.join(t, "bin")
    os.makedirs(dest_bin, exist_ok=True)
    shutil.copy2(r"d:\SVN\crm\Core.Cate\bin\Debug\Core.Cate.dll", os.path.join(dest_bin, "Core.Cate.dll"))
    if os.path.exists(r"d:\SVN\crm\Core.Cate\bin\Debug\Core.Cate.pdb"):
        shutil.copy2(r"d:\SVN\crm\Core.Cate\bin\Debug\Core.Cate.pdb", os.path.join(dest_bin, "Core.Cate.pdb"))
    shutil.copy2(r"d:\SVN\crm\Modules.Cate\bin\Modules.Cate.dll", os.path.join(dest_bin, "Modules.Cate.dll"))
    if os.path.exists(r"d:\SVN\crm\Modules.Cate\bin\Modules.Cate.pdb"):
        shutil.copy2(r"d:\SVN\crm\Modules.Cate\bin\Modules.Cate.pdb", os.path.join(dest_bin, "Modules.Cate.pdb"))

# Touch WebApp web.config to trigger reload
web_config = r"d:\SVN\crm\CenIT.Solution.TOC.WebApp\Web.config"
if os.path.exists(web_config):
    os.utime(web_config, None)
    print("Touched Web.config to reload IIS AppPool.")

print("Triple Mirroring synchronization completed successfully!")

# Verify hashes
for v in views_to_sync + css_files_to_sync:
    fname = os.path.basename(v)
    src_md5 = get_md5(v)
    pub_md5 = get_md5(os.path.join(r"d:\SVN\crm\publish_source\Areas\Cate\Views\DigitalSales", fname))
    web_md5 = get_md5(os.path.join(r"d:\SVN\crm\CenIT.Solution.TOC.WebApp\Areas\Cate\Views\DigitalSales", fname))
    print(f"File {fname}:")
    print(f"  Source  : {src_md5}")
    print(f"  Publish : {pub_md5}")
    print(f"  WebApp  : {web_md5}")
    assert src_md5 == pub_md5 == web_md5, f"MD5 mismatch for {fname}!"

print("ALL HASHES MATCH 100%!")
