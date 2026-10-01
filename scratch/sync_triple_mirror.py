import os
import shutil
import hashlib

files_to_sync = [
    # Dashboard views and scripts
    (r"Modules.Dashboard\Areas\Dashboard\Views\Dashboard\_Overview.cshtml",
     r"CenIT.Solution.TOC.WebApp\Areas\Dashboard\Views\Dashboard\_Overview.cshtml",
     r"publish_source\Areas\Dashboard\Views\Dashboard\_Overview.cshtml"),

    (r"Modules.Dashboard\Areas\Dashboard\Views\Dashboard\_ChartOverview.cshtml",
     r"CenIT.Solution.TOC.WebApp\Areas\Dashboard\Views\Dashboard\_ChartOverview.cshtml",
     r"publish_source\Areas\Dashboard\Views\Dashboard\_ChartOverview.cshtml"),

    (r"Modules.Dashboard\Areas\Dashboard\Views\Dashboard\Dashboard.js",
     r"CenIT.Solution.TOC.WebApp\Areas\Dashboard\Views\Dashboard\Dashboard.js",
     r"publish_source\Areas\Dashboard\Views\Dashboard\Dashboard.js"),

    (r"Modules.Dashboard\Areas\Dashboard\Views\Dashboard\DigitalSalesByStatus.cshtml",
     r"CenIT.Solution.TOC.WebApp\Areas\Dashboard\Views\Dashboard\DigitalSalesByStatus.cshtml",
     r"publish_source\Areas\Dashboard\Views\Dashboard\DigitalSalesByStatus.cshtml"),

    (r"Modules.Dashboard\Areas\Dashboard\Views\Dashboard\PlanByUser.cshtml",
     r"CenIT.Solution.TOC.WebApp\Areas\Dashboard\Views\Dashboard\PlanByUser.cshtml",
     r"publish_source\Areas\Dashboard\Views\Dashboard\PlanByUser.cshtml"),

    (r"Modules.Dashboard\Areas\Dashboard\Views\Dashboard\_OpportunityTable.cshtml",
     r"CenIT.Solution.TOC.WebApp\Areas\Dashboard\Views\Dashboard\_OpportunityTable.cshtml",
     r"publish_source\Areas\Dashboard\Views\Dashboard\_OpportunityTable.cshtml"),

    (r"Modules.Dashboard\Areas\Dashboard\Views\Dashboard\_RegulationModal.cshtml",
     r"CenIT.Solution.TOC.WebApp\Areas\Dashboard\Views\Dashboard\_RegulationModal.cshtml",
     r"publish_source\Areas\Dashboard\Views\Dashboard\_RegulationModal.cshtml"),

    (r"Modules.Dashboard\Areas\Dashboard\Views\Dashboard\Chart.js",
     r"CenIT.Solution.TOC.WebApp\Areas\Dashboard\Views\Dashboard\Chart.js",
     r"publish_source\Areas\Dashboard\Views\Dashboard\Chart.js"),

    # Cate views
    (r"Modules.Cate\Areas\Cate\Views\DigitalSales\_StatusTimelineDetailModal.cshtml",
     r"CenIT.Solution.TOC.WebApp\Areas\Cate\Views\DigitalSales\_StatusTimelineDetailModal.cshtml",
     r"publish_source\Areas\Cate\Views\DigitalSales\_StatusTimelineDetailModal.cshtml"),

    (r"Modules.Cate\Areas\Cate\Views\DigitalSales\_StatusTimelineModal.cshtml",
     r"CenIT.Solution.TOC.WebApp\Areas\Cate\Views\DigitalSales\_StatusTimelineModal.cshtml",
     r"publish_source\Areas\Cate\Views\DigitalSales\_StatusTimelineModal.cshtml"),

    (r"Modules.Cate\Areas\Cate\Views\ProjectTaskReport\_Report.cshtml",
     r"CenIT.Solution.TOC.WebApp\Areas\Cate\Views\ProjectTaskReport\_Report.cshtml",
     r"publish_source\Areas\Cate\Views\ProjectTaskReport\_Report.cshtml"),
]

base_dir = r"d:\SVN\crm"
BOM = b'\xef\xbb\xbf'

def ensure_bom_if_cshtml(filepath):
    if filepath.endswith('.cshtml'):
        with open(filepath, 'rb') as f:
            content = f.read()
        if not content.startswith(BOM):
            print(f"Adding UTF-8 BOM to {filepath}")
            with open(filepath, 'wb') as f:
                f.write(BOM + content)

def get_md5(filepath):
    if not os.path.exists(filepath):
        return None
    with open(filepath, 'rb') as f:
        return hashlib.md5(f.read()).hexdigest()

all_synced = True
for src_rel, dst1_rel, dst2_rel in files_to_sync:
    src_full = os.path.join(base_dir, src_rel)
    dst1_full = os.path.join(base_dir, dst1_rel)
    dst2_full = os.path.join(base_dir, dst2_rel)

    ensure_bom_if_cshtml(src_full)

    # Copy to WebApp
    os.makedirs(os.path.dirname(dst1_full), exist_ok=True)
    shutil.copy2(src_full, dst1_full)

    # Copy to publish_source
    os.makedirs(os.path.dirname(dst2_full), exist_ok=True)
    shutil.copy2(src_full, dst2_full)

    h_src = get_md5(src_full)
    h_dst1 = get_md5(dst1_full)
    h_dst2 = get_md5(dst2_full)

    if h_src == h_dst1 == h_dst2:
        print(f"MATCH: {src_rel} (MD5: {h_src})")
    else:
        print(f"MISMATCH: {src_rel} (src: {h_src}, dst1: {h_dst1}, dst2: {h_dst2})")
        all_synced = False

print(f"\nTriple Mirroring status: {'SUCCESS' if all_synced else 'FAILED'}")
