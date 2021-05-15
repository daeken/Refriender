using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNext.Collections.Generic;
using RefrienderCore;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace Rehexer {
	public class DataHexView : View {
		readonly IData Source;
		readonly int DisplayWidth, PositionNibbles;
		long _DisplayStart, _Position;

		readonly SortedList<long, (long Length, string Descriptor, Action Handler)> Blocks = new();
		readonly Dictionary<long, bool> ExpandedBlocks = new();
		long currentBlock = -1;

		public event Action<long> PositionChanged;

		/// <summary>
		/// Initialzies a <see cref="HexView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <param name="source">The <see cref="Stream"/> to view and edit as hex, this <see cref="Stream"/> must support seeking, or an exception will be thrown.</param>
		public DataHexView(IData source) {
			Source = source;
			CanFocus = true;
			PositionNibbles = source.Length switch {
				<= ushort.MaxValue => 4,
				<= uint.MaxValue => 8,
				<= 0xFFFF_FFFF_FF => 10,
				<= 0xFFFF_FFFF_FFFF => 12,
				<= 0xFFFF_FFFF_FFFF_FF => 14,
				_ => 16,
			};
			DisplayWidth = PositionNibbles + 1;
		}

		public void AddBlock(long offset, long size, string descriptor, Action handler = null) {
			if(size == 0) throw new NotSupportedException();
			Blocks.Add(offset, (size, descriptor, handler));
			if(offset <= _Position && offset + size > _Position)
				currentBlock = offset;
			ExpandedBlocks[offset] = false;
		}
		public void RemoveBlock(long offset) => Blocks.Remove(offset);
		public void ClearBlocks() => Blocks.Clear();

		internal void SetDisplayStart(long value) {
			if(value >= Source.Length)
				_DisplayStart = Source.Length - 1;
			else if(value < 0)
				_DisplayStart = 0;
			else
				_DisplayStart = value;
			SetNeedsDisplay();
		}

		/// <summary>
		/// Sets or gets the offset into the <see cref="Stream"/> that will displayed at the top of the <see cref="HexView"/>
		/// </summary>
		/// <value>The display start.</value>
		public long DisplayStart {
			get => _DisplayStart;
			set {
				_Position = value;

				SetDisplayStart(value);
			}
		}

		long AlignDown(long v) => v % bytesPerLine is var o and > 0 ? v - o : v;
		long AlignUp(long v) => v % bytesPerLine is var o and > 0 ? v + (bytesPerLine - o) : v;

		public long Position {
			get => _Position;
			set {
				var npos = Math.Max(0, Math.Min(value, Source.Length - 1));
				if(_Position == npos) return;
				var po = AlignDown(npos);
				var halfsize = AlignDown(bytesPerLine * Frame.Height / 2);
				if(po + halfsize > Source.Length)
					DisplayStart = AlignUp(Source.Length) - bytesPerLine * (Frame.Height - 1);
				else if(po - halfsize < 0)
					DisplayStart = 0;
				else
					DisplayStart = po - halfsize;
				_Position = npos;
				SetNeedsDisplay();
				PositionChanged?.Invoke(_Position);
			}
		}

		const int bsize = 4;
		int bytesPerLine;

		/// <inheritdoc/>
		public override Rect Frame {
			get => base.Frame;
			set {
				base.Frame = value;

				// Small buffers will just show the position, with 8 bytes
				bytesPerLine = 8;
				if(value.Width - DisplayWidth > 67)
					bytesPerLine = 16 * ((value.Width - DisplayWidth) / (16 * 4 + 2 + 1 + 2 + 1));
			}
		}

		///<inheritdoc/>
		public override void Redraw(Rect bounds) {
			var currentAttribute = ColorScheme.Focus;
			Driver.SetAttribute(currentAttribute);
			Move(0, 0);

			var frame = Frame;

			var nchunks = bytesPerLine / 8;
			var dlen = nchunks * 8 * frame.Height;

			int activeColor = ColorScheme.HotNormal;
			int trackingColor = ColorScheme.HotFocus;

			var cpos = _DisplayStart;

			for(var line = 0; line < frame.Height && cpos <= Source.Length; line++) {
				var lineRect = new Rect(0, line, frame.Width, 1);
				if(!bounds.Contains(lineRect))
					continue;

				Move(0, line);

				if(Blocks.TryGetValue(cpos, out var block)) {
					SetAttribute(cpos <= _Position && cpos + block.Length > _Position ? activeColor : ColorScheme.Normal);
					Driver.AddStr(new string(' ', PositionNibbles + 1));
					Driver.AddStr($"«Block 0x{cpos:X}-0x{cpos+block.Length-1:X} -- {block.Descriptor}»");
					cpos += block.Length;
				} else {
					var apos = AlignDown(cpos);
					var shift = (int) (cpos - apos);
					var data = Source.Slice(cpos, bytesPerLine - shift).Span;
					var n = data.Length;
					var (key, _) = Blocks.FirstOrDefault(x => x.Key > cpos);
					if(key != 0 && key < cpos + bytesPerLine - shift)
						n = (int) (key - cpos);

					SetAttribute(ColorScheme.HotNormal);
					Driver.AddStr(Math.Min(Source.Length, cpos).ToString($"X{PositionNibbles}") + " ");

					SetAttribute(ColorScheme.Normal);


					for(var chunk = 0; chunk < nchunks; chunk++) {
						for(var b = 0; b < 8; b++) {
							var offset = chunk * 8 + b;
							var value = offset < shift || offset - shift >= n ? 0 : data[offset - shift];
							if(offset + apos == _Position)
								SetAttribute(activeColor);
							else
								SetAttribute(ColorScheme.Normal);

							Driver.AddStr(offset < shift || offset - shift >= n ? "  " : $"{value:x2}");
							SetAttribute(ColorScheme.Normal);
							Driver.AddRune(' ');
						}

						Driver.AddStr(chunk % 2 == 1 && chunk != nchunks - 1 ? "  " : " ");
					}
					
					for(var bitem = 0; bitem < nchunks * 8; bitem++) {
						if(bitem % 8 == 0 && bitem != 0) {
							var chunk = bitem / 8;
							Driver.AddStr(chunk % 2 == 0 && chunk != nchunks - 1 ? "  " : " ");
						}

						if(bitem + apos == _Position)
							SetAttribute(activeColor);
						else
							SetAttribute(ColorScheme.Normal);

						Driver.AddRune(bitem < shift || bitem - shift >= n ? ' ' : data[bitem - shift] switch { < 32 or > 127 => '.', var b => b });
					}

					cpos += n;
				}
			}

			void SetAttribute(Attribute attribute) {
				if(currentAttribute != attribute) {
					currentAttribute = attribute;
					Driver.SetAttribute(attribute);
				}
			}
		}

		///<inheritdoc/>
		public override void PositionCursor() {
			if(bytesPerLine < 16) return; // TODO: Make small buffers not completely fucked
			var delta = (int) (_Position - _DisplayStart);
			var line = delta / bytesPerLine;
			var item = delta % bytesPerLine;
			var block = item / 8;
			var shift = block / 2 + block;
			
			var dsize = bytesPerLine / 16 * (16 * 3 + 2 + 1) - 1;

			Move(DisplayWidth + dsize + item + shift, line);
		}

		void RedisplayLine(long pos) {
			var delta = (int) (pos - DisplayStart);
			var line = delta / bytesPerLine;

			SetNeedsDisplay(new Rect(0, line, Frame.Width, 1));
		}

		void CursorRight() {
			RedisplayLine(_Position);
			if(_Position < Source.Length - 1)
				_Position++;
			if(_Position == Source.Length - 1) {
				Position = _Position;
				return;
			}
			PositionChanged?.Invoke(_Position);
			if(_Position >= (DisplayStart + bytesPerLine * Frame.Height)) {
				SetDisplayStart(DisplayStart + bytesPerLine);
				SetNeedsDisplay();
			} else
				RedisplayLine(_Position);
		}

		void MoveUp(int bytes) {
			RedisplayLine(_Position);
			_Position -= bytes;
			if(_Position < 0)
				_Position = 0;
			PositionChanged?.Invoke(_Position);
			if(_Position < DisplayStart) {
				SetDisplayStart(DisplayStart - bytes);
				SetNeedsDisplay();
			} else
				RedisplayLine(_Position);
		}

		void MoveDown(int bytes) {
			RedisplayLine(_Position);
			
			if(_Position + bytes < Source.Length)
				_Position += bytes;
			else {
				Position = Source.Length - 1;
				return;
			}
			PositionChanged?.Invoke(_Position);
			if(_Position >= (DisplayStart + bytesPerLine * Frame.Height)) {
				SetDisplayStart(DisplayStart + bytes);
				SetNeedsDisplay();
			} else
				RedisplayLine(_Position);
		}

		/// <inheritdoc/>
		public override bool ProcessKey(KeyEvent keyEvent) {
			switch(keyEvent.Key) {
				case Key.CursorLeft:
					RedisplayLine(_Position);
					if(_Position == 0)
						return true;
					if(_Position - 1 < DisplayStart) {
						SetDisplayStart(_DisplayStart - bytesPerLine);
						SetNeedsDisplay();
					} else
						RedisplayLine(_Position);

					_Position--;
					PositionChanged?.Invoke(_Position);
					break;
				case Key.CursorRight:
					CursorRight();
					break;
				case Key.CursorDown:
					MoveDown(bytesPerLine);
					break;
				case Key.CursorUp:
					MoveUp(bytesPerLine);
					break;
				case ('v' + Key.AltMask):
				case Key.PageUp:
					MoveUp(bytesPerLine * Frame.Height);
					break;
				case Key.V | Key.CtrlMask:
				case Key.PageDown:
					MoveDown(bytesPerLine * Frame.Height);
					break;
				case Key.Home:
				case (Key) '<':
					Position = 0;
					break;
				case Key.End:
				case (Key) '>':
					Position = Source.Length - 1;
					break;
				case Key.Enter when currentBlock != -1:
					Blocks[currentBlock].Handler();
					break;
				default:
					return false;
			}

			currentBlock = Blocks.Where(x => x.Key <= _Position && x.Key + x.Value.Length > _Position).FirstOrNull()?.Key ?? -1;

			PositionCursor();
			return true;
		}

		CursorVisibility desiredCursorVisibility = CursorVisibility.Default;

		/// <summary>
		/// Get / Set the wished cursor when the field is focused
		/// </summary>
		public CursorVisibility DesiredCursorVisibility {
			get => desiredCursorVisibility;
			set {
				if(desiredCursorVisibility != value && HasFocus) Application.Driver.SetCursorVisibility(value);

				desiredCursorVisibility = value;
			}
		}
	}
}