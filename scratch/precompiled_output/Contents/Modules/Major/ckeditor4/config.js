/**
 * @license Copyright (c) 2003-2022, CKSource Holding sp. z o.o. All rights reserved.
 * For licensing, see https://ckeditor.com/legal/ckeditor-oss-license
 */

CKEDITOR.config.allowedContent = true;

CKEDITOR.editorConfig = function( config ) {
	// Define changes to default configuration here. For example:
	// config.language = 'fr';
    // config.uiColor = '#AADC6E';
    config.syntaxhighlight_lang = 'csharp';
    config.syntaxhighlight_hideControls = true;
    config.languages = 'vi';
    config.fileSize = 0;
    config.allowMutiple = true;
    config.selectMultiple = true;
    config.fileType = "AllFile";
    config.filebrowserBrowseUrl = '/Contents/Modules/Major/ckfinder/ckfinder.html';
    config.filebrowserImageBrowseUrl = '/Contents/Modules/Major/ckfinder/ckfinder.html?Types=Images&currentFolder=/Contents/imgs/';
    config.filebrowserFlashBrowseUrl = '/Contents/Modules/Major/ckfinder/ckfinder.html?Types=Flash';
    config.filebrowserUploadUrl = '/Contents/Modules/Major/ckfinder/core/connector/aspx/connector.aspx?command=QuickUpload&type=File';
    config.filebrowserImageUploadUrl = '/Contents/Modules/Major/ckfinder/core/connector/aspx/connector.aspx?command=QuickUpload&type=Images',
        config.filebrowserFlashUploadUrl = '/Contents/Modules/Major/ckfinder/core/connector/aspx/connector.aspx?command=QuickUpload&type=Flash';
    CKFinder.setupCKEditor(null, '/Contents/Modules/Major/ckfinder/');
    config.filebrowserWindowWidth = "800"; // "Browse Server" pop-up box size set
    config.filebrowserWindowHeight = "800";
    config.height = "500";
     // Referencing the new plugin
    config.extraPlugins = 'newplugin';
};