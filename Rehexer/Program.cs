using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using RefrienderCore;
using Rehexer;
using Terminal.Gui;

Application.Init();
var top = Application.Top;

var fn = args[0];
var data = new DataMapped(fn);

var win = new Window(Path.GetFileName(fn)) {
	X = 0, Y = 1, 
	Width = Dim.Fill(), Height = Dim.Fill() - 1
};
var hv = new DataHexView(data) {
	X = 0, Y = 0, 
	Width = Dim.Fill(), Height = Dim.Fill()
};
var hvstack = new Stack<DataHexView>();
win.Add(hv);
top.Add(win);

top.Add(new MenuBar(new MenuBarItem[] {
	new("_File", new MenuItem [] {
		new("_New", "Creates new file", null),
		new("_Close", "",null),
		new("_Quit", "", () => {
			var n = MessageBox.Query(50, 7, "Quit Demo", "Are you sure you want to quit this demo?", "Yes", "No");
			if(n == 0) top.Running = false;
		})
	}),
	new("_Edit", new MenuItem [] {
		new("_Copy", "", null),
		new("C_ut", "", null),
		new("_Paste", "", null)
	})
}));

var positionItem = new StatusItem(Key.Null, "Position", null);
void registerHv() =>
	hv.PositionChanged += position => positionItem.Title = $"Offset 0x{position:X}";
registerHv();

top.Add(new StatusBar(new []{ positionItem }) {
	X = Pos.At(100), 
	Width = Dim.Percent(50), 
});

top.KeyPress += e => {
	switch(e.KeyEvent.Key) {
		case Key.G:
		case Key.Space | Key.G: // Why are we getting ORed with Space here?
			AskText()
				.Option("Go to offset (hex)", offset => {
					var toffset = offset.Trim().Replace(" ", "").Replace("_", "");
					if(!long.TryParse(toffset, NumberStyles.HexNumber, null, out var res))
						return false;
					hv.Position = res;
					return true;
				})
				.Option("Go to offset (decimal)", offset => {
					var toffset = offset.Trim().Replace(" ", "").Replace("_", "");
					if(!long.TryParse(toffset, out var res))
						return false;
					hv.Position = res;
					return true;
				})
				.Option("Go to offset (Python)", offset => {
					return false;
				})
				.CancelOnEmptyEnter();
			break;
		case Key.Esc when hvstack.Count != 0:
			win.Remove(hv);
			win.Add(hv = hvstack.Pop());
			break;
		default: return;
	}
	e.Handled = true;
};

var cf = new CompressionFinder(data, minLength: 128, algorithms: CompressionAlgorithm.Zlib, positionOnly: false, removeOverlapping: true, logLevel: 0);
cf.Blocks.ForEach(block => hv.AddBlock(block.Offset, block.CompressedLength,
	$"{block.Algorithm} 0x{block.DecompressedLength:X} bytes", () => {
		hvstack.Push(hv);
		win.Remove(hv);
		win.Add(hv = new DataHexView(new DataBytes(cf.Decompress(block))) { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() });
		registerHv();
	}));

Application.Run();

ModalAskText AskText() {
	var cfocus = top.Focused;
	var mat = new ModalAskText(cfocus);
	top.Add(mat);
	top.LayoutSubviews();
	mat.SetFocus();
	return mat;
}
