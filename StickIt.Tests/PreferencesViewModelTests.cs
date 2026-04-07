using System;

using StickIt.Persistence;

namespace StickIt.Tests
{
   public class PreferencesViewModelTests
   {
      [Fact]
      public void ToPreferences_NormalizesAutoListAndSyncPathValues()
      {
         var vm = PreferencesViewModel.FromPreferences(new AppPreferences
         {
            AutoListBulletSymbol = "",
            AutoListSpacesAfterMarker = 99,
            AutoListNumberSuffix = "   "
         });

         vm.SyncFilePath = "  C:\\Sync\\StickIt.3m  ";

         var prefs = vm.ToPreferences();

         Assert.Equal("•", prefs.AutoListBulletSymbol);
         Assert.Equal(4, prefs.AutoListSpacesAfterMarker);
         Assert.Equal(".", prefs.AutoListNumberSuffix);
         Assert.Equal("C:\\Sync\\StickIt.3m", prefs.SyncFilePath);
      }

      [Fact]
      public void FromPreferences_WithNull_UsesSafeDefaultsAndSummaries()
      {
         var vm = PreferencesViewModel.FromPreferences(null!);

         Assert.Equal("Not set", vm.DesktopAreaSummary);
         Assert.Equal("Never", vm.LastSyncSummary);
         Assert.Contains("Task item", vm.AutoListSample, StringComparison.Ordinal);
      }

      [Fact]
      public void DesktopAreaSummary_RefreshesWhenAreaChanges()
      {
         var vm = PreferencesViewModel.FromPreferences(new AppPreferences());

         vm.DesktopAreaLeft = 10;
         vm.DesktopAreaTop = 20;
         vm.DesktopAreaWidth = 300;
         vm.DesktopAreaHeight = 200;

         Assert.Equal("Left 10, Top 20, 300 x 200", vm.DesktopAreaSummary);
      }

      [Fact]
      public void ToPreferences_PreservesMode2AndSyncEnums()
      {
         var vm = PreferencesViewModel.FromPreferences(new AppPreferences());

         vm.Mode2HostMissingAction = Mode2HostMissingAction.SwitchToMode0;
         vm.SyncMode = SyncMode.AlwaysPush;
         vm.SyncImportMode = SyncImportMode.MergeByNoteIdNewestWins;

         var prefs = vm.ToPreferences();

         Assert.Equal(Mode2HostMissingAction.SwitchToMode0, prefs.Mode2HostMissingAction);
         Assert.Equal(SyncMode.AlwaysPush, prefs.SyncMode);
         Assert.Equal(SyncImportMode.MergeByNoteIdNewestWins, prefs.SyncImportMode);
      }

      [Fact]
      public void AutoListSample_UsesClampedSpacingAndFallbackMarkers()
      {
         var vm = PreferencesViewModel.FromPreferences(new AppPreferences());

         vm.AutoListBulletSymbol = "";
         vm.AutoListNumberSuffix = "";
         vm.AutoListSpacesAfterMarker = 0;
         Assert.StartsWith("• Task item", vm.AutoListSample, StringComparison.Ordinal);
         Assert.Contains("1. Numbered item", vm.AutoListSample, StringComparison.Ordinal);

         vm.AutoListSpacesAfterMarker = 4;
         Assert.StartsWith("•    Task item", vm.AutoListSample, StringComparison.Ordinal);
      }

      [Fact]
      public void LastSyncSummary_RefreshesWhenTimestampChanges()
      {
         var vm = PreferencesViewModel.FromPreferences(new AppPreferences());
         Assert.Equal("Never", vm.LastSyncSummary);

         vm.LastSyncUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

         Assert.NotEqual("Never", vm.LastSyncSummary);
      }
   }
}
