using System;
using System.Collections.Generic;
using NStack;
using Terminal.Gui;

namespace Rehexer {
	public class ModalAskText : Toplevel {
		bool cancelOnEmptyEnter;

		readonly Label CommandLabel;
		readonly TextField TextField;
		readonly List<(string Label, Func<string, bool> Action)> Actions = new();
		int CurAction = 0;
		
		public ModalAskText(View curFocus) {
			CanFocus = true;
			Modal = true;
			X = 0;
			Height = 1;
			Width = Dim.Percent(50);
			CommandLabel = new Label { X = 0, Y = 0, Height = 1 };
			Add(CommandLabel);
			TextField = new TextField { X = Pos.Right(CommandLabel) + 1, Y = 0, Width = Dim.Fill(), Height = 1 };
			Add(TextField);
			TextField.FocusFirst();

			Initialized += (_, _) => Y = SuperView.Frame.Height - 1;
			Application.Resized += _ => {
				X = 0;
				Height = 1;
				if(SuperView is { } or Toplevel)
					Y = SuperView.Frame.Height - (Visible ? 1 : 0);
			};

			KeyPress += e => {
				switch(e.KeyEvent.Key) {
					case Key.Enter:
						e.Handled = true;
						if(cancelOnEmptyEnter && TextField.Text == ustring.Empty ||
						   Actions[CurAction].Action(TextField.Text.ToString() ?? "")) {
							SuperView.Remove(this);
							curFocus.SetFocus();
						}

						break;
					case Key.Tab:
						e.Handled = true;
						CurAction = (CurAction + 1) % Actions.Count;
						var clabel = Actions[CurAction].Label;
						CommandLabel.Text = clabel;
						CommandLabel.Width = clabel.Length;
						break;
				}
			};
		}

		public ModalAskText Option(string name, Action<string> action) =>
			Option(name, x => {
				action(x);
				return true;
			});

		public ModalAskText Option(string name, Func<string, bool> action) {
			if(CommandLabel.Text == ustring.Empty)
				CommandLabel.Text = name;
			Actions.Add((name, action));
			return this;
		}

		public ModalAskText CancelOnEmptyEnter() {
			cancelOnEmptyEnter = true;
			return this;
		}
	}
}