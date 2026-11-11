using Godot;
using System;
using System.Collections.Generic;

public partial class Tutorial : Control
{
	private struct Slide {
		public string words;
		public string image;

		public int newlines;

	}
	TextureRect slide_tex;
	Label dialog;

	Timer text_scan_timer;
	
	List<Slide> slides = new List<Slide>();
	int slide_i = -1;

	int text_scan_i = 0;

	public override void _Ready()
	{
		slide_tex = GetNode<TextureRect>("VBoxContainer/AspectRatioContainer/Scroll/Slide");
		dialog = GetNode<Label>("VBoxContainer/Dialog");
		text_scan_timer = dialog.GetNode<Timer>("ScanTimer");
	}


	private void _on_next_btn_button_up()
	{
		if (text_scan_i < slides[slide_i].words.Length) {
			skipidi();
		} else
			nextSlide();
	}


	private void _on_back_btn_button_up()
	{
		if (text_scan_i < slides[slide_i].words.Length) {
			skipidi();
		} else
			lastSlide();
	}

	private void skipidi() {
		Slide slide = slides[slide_i];
		if (text_scan_i < slide.words.Length) {
			text_scan_i = slide.words.Length;
			dialog.Text = "Oracle Yenna\n" + slide.words.Substr(0, text_scan_i);
			addNewlines(slide);
		}
	}

	public void loadSlides(string tutfile) {
		slide_i = -1;
		slides.Clear();
		using ( var slides_txt = FileAccess.Open(OldConst.STATIC_PATH + "tutorial/" + tutfile + ".txt", FileAccess.ModeFlags.Read) ) {
			string line; 
			while(!slides_txt.EofReached() && (line = slides_txt.GetLine()) != null) {
				string[] split = line.Trim().Split("//");
				slides.Add( new Slide{ words = split[0].Replace("\\n", "\n"),
							image = split[1], 
							newlines = int.Parse(split[2])} );
			}
		}
		nextSlide();
	}

	private void changeSlide() {
		text_scan_i = 0;
		Slide slide = slides[slide_i];
		dialog.Text = "Oracle Yenna\n";
		addNewlines(slide);
		if (slide.image.Length > 1) {
			((TextureRect)slide_tex.GetParent()).Show();
			slide_tex.Texture = ResourceLoader.Load<Texture2D>( "res://resources/2d/tutorial/" + slide.image + ".jpg" );
		} else {
			((TextureRect)slide_tex.GetParent()).Hide();
			slide_tex.Texture = null;
		}
		text_scan_timer.Start(0.02);
	}

	private void nextSlide()
	{
		slide_i += 1;
		if (slide_i < slides.Count) {
			changeSlide();
		} else 
			QueueFree();
	}

	private void lastSlide() {
		slide_i -= 1;
		if (slide_i > -1) {
			changeSlide();
		} else 
			slide_i = 0;
	}
	
	private void _on_close_button_up()
	{
		QueueFree();
	}
	
	private void _on_scan_timer_timeout()
	{
		Slide slide = slides[slide_i];
		if (text_scan_i + 1 <= slide.words.Length) {
			text_scan_i += 1;
			dialog.Text = "Oracle Yenna\n" + slide.words.Substr(0, text_scan_i);
			addNewlines(slide);
			text_scan_timer.Start(0.02);
		}
	}

	private void addNewlines(Slide slide) {
		string newlines = "";
		for (int i = 0; i < slide.newlines; i++)
			newlines += "\n";
		dialog.Text += newlines;
	}
}









