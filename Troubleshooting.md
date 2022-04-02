# Troubleshooting

## Save files are not detected for a game

1) Verify that the game's page on [PCGamingWiki](https://www.pcgamingwiki.com/wiki/Home) contains entries for the save data and/or config data paths

2) Ensure the game name in Playnite exactly matches the above PCGamingWiki game's name

3) Verify that the game save location data has been added to [ludusavi manifest](https://raw.githubusercontent.com/mtkennerly/ludusavi-manifest/master/data/manifest.yaml). This is automatically updated daily

4) Ensure you have "roots" configured in Ludusavi for launcher (`Uplay`, `Steam`, etc). This is particularly important when the game saves are located in the game's installation directory.



## Files missing from snapshot

Run ludusavi backup preview to ensure all expected files are detected:

```
> ludusavi backup --preview --api "Elden Ring"

{                                                                                                                                                                                                                                                                           1
  "overall": {
    "totalGames": 1,
    "totalBytes": 28970290,
    "processedGames": 1,
    "processedBytes": 28970290
  },
  "games": {
    "Elden Ring": {
      "decision": "Processed",
      "files": {
        "C:/Users/marcus/AppData/Roaming/EldenRing/<userid>/ER0000.sl2": {
          "bytes": 28967888
        },
        "C:/Users/marcus/AppData/Roaming/EldenRing/GraphicsConfig.xml": {
          "bytes": 2402
        }
      },
      "registry": {}
    }
  }
}
```

Check that all of the above paths were included in the latest snapshot

```
> restic snapshots -r rclone:restic:/gamesaves --tag "Elden Ring" --latest 1

repository c495813c opened successfully, password is correct
ID        Time                 Host            Tags        Paths
---------------------------------------------------------------------------------------------------------------------------------
a3ffc31b  2022-02-25 22:26:53  GAMING-DESKTOP  Elden Ring  C:\Users\marcus\AppData\Roaming\EldenRing\<userid>\ER0000.sl2
                                                           C:\Users\marcus\AppData\Roaming\EldenRing\GraphicsConfig.xml
---------------------------------------------------------------------------------------------------------------------------------
1 snapshots
```
