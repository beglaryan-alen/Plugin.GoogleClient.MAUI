﻿using GoogleClientSample.Views;

namespace GoogleClientSample;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new MainPage();
	}
}
