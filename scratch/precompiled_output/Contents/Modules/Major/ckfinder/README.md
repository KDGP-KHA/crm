1. Tạo trong project WebApp thư mục "/Uploads/Images/"
2. Copy ckfinder vào thư mục "/Contents/plugins/ckfinder/"
3. Tạo license key https://www.charmevietnam.com/Common/CkfinderKey
4. Thêm file CKfinder.dll ở trong thư mục "/Contents/plugins/ckfinder/bin/" bỏ vào reference của project WebApp
5. Thêm javascript vào như sau

<script type="text/javascript" src="@Url.Content("~/Contents/plugins/ckfinder/ckfinder.js")"></script>
<script type="text/javascript" src="@Url.Content("~/Contents/plugins/ckfinder/jquery.js")"></script>

<script type="text/javascript">
    $(function () {
        $('#btnSelectImage').click(function () {
            var ckfinder = new CKFinder();
            ckfinder.selectActionFunction = function (Images) {
                $('#m_image').val(Images);

            };
            ckfinder.popup();
        });

        CKEDITOR.replace("m_detail", { customConfig: "/Contents/plugins/ckeditor4/config.js" });
    });
</script>