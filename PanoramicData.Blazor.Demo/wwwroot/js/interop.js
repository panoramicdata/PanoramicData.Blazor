window.panoramicDataDemo = {

	downloadFiles: function (args) {
		for (var i = 0; i < args.items.length; i++) {
			var url = "/files/download?path=" + args.items[i].path;
			panoramicData.downloadFromUrl(url, args.items[i].name);
		}
	},


}