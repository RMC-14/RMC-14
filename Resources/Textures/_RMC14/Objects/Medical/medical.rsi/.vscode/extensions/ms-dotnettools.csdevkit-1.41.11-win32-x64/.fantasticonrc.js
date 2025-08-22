const { FontAssetType } = require('fantasticon')

module.exports = {
	name: 'csharp-dev-kit-icon-font',
	prefix: 'csharp-dev-kit-icon-font',
	inputDir: './media/icon-font',
	outputDir: './media',
	fontTypes: [FontAssetType.WOFF],
	assetTypes: [], // by default it will generate additional assets like ts mapping and html with icons demo
	normalize: true,
	codepoints: {
		'statusbar-no-entitlement': 65, // mapping between the svg file name and corresponding character in the font
		'statusbar-attention-needed': 66,
		'statusbar-entitlement': 67
	}
};