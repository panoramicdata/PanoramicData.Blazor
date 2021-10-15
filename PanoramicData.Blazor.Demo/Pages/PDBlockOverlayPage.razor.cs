﻿using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Services;
using System;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDBlockOverlayPage
	{
		[Inject] protected IBlockOverlayService BlockOverlayService { get; set; } = null!;

		protected async Task ShowFor1SecondAsync()
		{
			BlockOverlayService.Show();
			try
			{
				// Do some quick operation
				await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(true);
			}
			finally
			{
				BlockOverlayService.Hide();
			}
		}

		protected async Task ShowFor3SecondsAsync()
		{
			BlockOverlayService.Show("A longer 3 second operation");
			try
			{
				// Do some longer operation
				await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(true);
			}
			finally
			{
				BlockOverlayService.Hide();
			}
		}
	}
}
