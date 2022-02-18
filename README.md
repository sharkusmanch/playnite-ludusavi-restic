# Ludusavi Restic Playnite Plugin

Plugin for [Playnite](https://playnite.link) that allows for creating point-in-time snapshots of game save data.

Game save files are sourced from [Ludusavi](https://github.com/mtkennerly/ludusavi) and snapshot backups are created using [restic](https://github.com/restic/restic)

Goal of this project is to provide a thin layer connecting these two existing tools.

## Features

- Automatically create snapshots of save data when game is stopped
- Create snapshot backups of save data on demand for selected games
- Backup to many different cloud providers supported by `restic` and `rclone`. See [available rclone backends](https://rclone.org/#providers) for available `rclone` backends
- Each snapshot is tagged according to the game it belongs to, so snapshots of different games can be managed independently.

## Prerequisites

- This plugin requires that `ludusavi` and `restic` are installed.
    - [ludusavi installation doc](https://github.com/mtkennerly/ludusavi#installation)
    - [restic installation doc](https://restic.readthedocs.io/en/latest/020_installation.html)
- [rclone](https://github.com/rclone/rclone) may be optionally installed and configured to allow many more cloud backends to
- A restic repository designated to host snapshots of game save data.

## Getting Started

- See restic docs for [preparing a new repo](https://restic.readthedocs.io/en/stable/030_preparing_a_new_repo.html).

- Create a restic repository (using local disk repo for example), be sure to remember/save your password:
```
PS > restic init -r C:\Users\marcus\Desktop\testrepo
enter password for new repository:
enter password again:
created restic repository ed411a7154 at C:\Users\marcus\Desktop\testrepo

Please note that knowledge of your password is required to access
the repository. Losing your password means that your data is
irrecoverably lost.
```

- In Playnite plugin settings for `Ludusavi Restic`, supply the required variables: restic repository, restic password

- Update optional variables as needed: rclone config, rclone config password, path to `ludusavi`, path to `restic`

## View backups

View data and snapshots stored in the game data repository is handled by `restic`

See restic's docs for [working with repositories](https://restic.readthedocs.io/en/stable/045_working_with_repos.html)

Example listing snapshots:
```
PS C:\Users\marcus> restic snapshots --compact -r rclone:restic:/gamesaves
repository c495813c opened successfully, password is correct
ID        Time                 Host             Tags
-----------------------------------------------------------------------------------------
af439279  2021-08-09 11:57:35  GAMING-DESKTOP   Dreamscaper
a205d890  2021-08-09 11:57:51  GAMING-DESKTOP   Horizon Zero Dawn
ea209030  2021-08-09 11:57:55  GAMING-DESKTOP   Dreamscaper
d8ce2c32  2021-08-09 11:57:58  GAMING-DESKTOP   Death's Door
70847301  2021-08-09 11:58:02  GAMING-DESKTOP   Monster Hunter Stories 2: Wings of Ruin
ff35fa94  2021-08-09 12:00:08  GAMING-DESKTOP   Monster Hunter Stories 2: Wings of Ruin
2b5ebb33  2021-08-09 12:00:11  GAMING-DESKTOP   Death's Door
```

List backups for a specific game:
```
PS C:\Users\marcus> restic snapshots --compact -r rclone:restic:/gamesaves --tag "Death's Door"
repository c495813c opened successfully, password is correct
ID        Time                 Host            Tags
-----------------------------------------------------------
44a02603  2021-08-07 12:52:05  GAMING-DESKTOP  Death's Door
91deb969  2021-08-07 13:00:26  GAMING-DESKTOP  Death's Door
0f055a90  2021-08-09 10:57:16  GAMING-DESKTOP  Death's Door
d8ce2c32  2021-08-09 11:57:58  GAMING-DESKTOP  Death's Door
2b5ebb33  2021-08-09 12:00:11  GAMING-DESKTOP  Death's Door
-----------------------------------------------------------
5 snapshots
```


## Help with localization

https://crowdin.com/project/playnite-ludusavi-restic
