﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Providers.Jobs;
using NzbDrone.Core.Repository;
using NzbDrone.Web.Models;
using Telerik.Web.Mvc;

namespace NzbDrone.Web.Controllers
{
    [HandleError]
    public class SeriesController : Controller
    {
        private readonly EpisodeProvider _episodeProvider;
        private readonly QualityProvider _qualityProvider;
        private readonly RenameProvider _renameProvider;
        private readonly SeriesProvider _seriesProvider;
        private readonly TvDbProvider _tvDbProvider;
        private readonly JobProvider _jobProvider;
        private readonly MediaFileProvider _mediaFileProvider;
        //
        // GET: /Series/

        public SeriesController(SeriesProvider seriesProvider,
                                EpisodeProvider episodeProvider,
                                QualityProvider qualityProvider,
                                RenameProvider renameProvider,
                                TvDbProvider tvDbProvider,
                                JobProvider jobProvider,
                                MediaFileProvider mediaFileProvider)
        {
            _seriesProvider = seriesProvider;
            _episodeProvider = episodeProvider;
            _qualityProvider = qualityProvider;
            _renameProvider = renameProvider;
            _tvDbProvider = tvDbProvider;
            _jobProvider = jobProvider;
            _mediaFileProvider = mediaFileProvider;
        }

        public ActionResult Index()
        {
            var profiles = _qualityProvider.GetAllProfiles();
            ViewData["SelectList"] = new SelectList(profiles, "QualityProfileId", "Name");

            return View();
        }

        public ActionResult RssSync()
        {
            _jobProvider.QueueJob(typeof(RssSyncJob));
            return RedirectToAction("Index");
        }

        public ActionResult SeasonEditor(int seriesId)
        {
            var model = new List<SeasonEditModel>();

            var seasons = _episodeProvider.GetSeasons(seriesId);

            foreach (var season in seasons)
            {
                var seasonEdit = new SeasonEditModel();
                seasonEdit.Monitored = !_episodeProvider.IsIgnored(seriesId, season);
                seasonEdit.SeasonNumber = season;
                seasonEdit.SeriesId = seriesId;
                seasonEdit.SeasonString = GetSeasonString(season);

                model.Add(seasonEdit);
            }

            return View(model);
        }

        public ActionResult GetSingleSeasonView(SeasonEditModel model)
        {
            return PartialView("SingleSeason", model);
        }

        [GridAction]
        public ActionResult _AjaxSeriesGrid()
        {
            var series = GetSeriesModels(_seriesProvider.GetAllSeries().ToList());

            return View(new GridModel(series));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        [GridAction]
        public ActionResult _SaveAjaxSeriesEditing(int id, string path, bool monitored, bool seasonFolder, int qualityProfileId, List<SeasonEditModel> seasons)
        {
            var oldSeries = _seriesProvider.GetSeries(id);
            oldSeries.Path = path;
            oldSeries.Monitored = monitored;
            oldSeries.SeasonFolder = seasonFolder;
            oldSeries.QualityProfileId = qualityProfileId;

            _seriesProvider.UpdateSeries(oldSeries);

            var series = GetSeriesModels(_seriesProvider.GetAllSeries().ToList());
            return View(new GridModel(series));
        }

        [GridAction]
        public ActionResult _DeleteAjaxSeriesEditing(int id)
        {
            //Grab the series from the DB so we can remove it from the list we return to the client
            var seriesInDb = _seriesProvider.GetAllSeries().ToList();

            //Remove this so we don't send it back to the client (since it hasn't really been deleted yet)
            seriesInDb.RemoveAll(s => s.SeriesId == id);

            //Start removing this series
            _jobProvider.QueueJob(typeof(DeleteSeriesJob), id);

            var series = GetSeriesModels(seriesInDb);
            return View(new GridModel(series));
        }

        [GridAction]
        public ActionResult _AjaxSeasonGrid(int seriesId, int seasonNumber)
        {
            var episodes = _episodeProvider.GetEpisodesBySeason(seriesId, seasonNumber).Select(c => new EpisodeModel
                                                                                         {
                                                                                             EpisodeId = c.EpisodeId,
                                                                                             EpisodeNumber = c.EpisodeNumber,
                                                                                             SeasonNumber = c.SeasonNumber,
                                                                                             Title = c.Title,
                                                                                             Overview = c.Overview,
                                                                                             AirDate = c.AirDate,
                                                                                             Path = GetEpisodePath(c.EpisodeFile),
                                                                                             Status = c.Status.ToString(),
                                                                                             Quality = c.EpisodeFile == null
                                                                                                     ? String.Empty
                                                                                                     : c.EpisodeFile.Quality.ToString()
                                                                                         });
            return View(new GridModel(episodes));
        }

        public JsonResult GetEpisodeCount(int seriesId)
        {
            var count = _mediaFileProvider.GetEpisodeFilesCount(seriesId);

            return Json(new
            {
                Episodes = count.Item1,
                EpisodeTotal = count.Item2
            }, JsonRequestBehavior.AllowGet);
        }

        //Local Helpers
        private string GetEpisodePath(EpisodeFile file)
        {
            if (file == null)
                return String.Empty;

            //Return the path relative to the Series' Folder
            return file.Path;
        }

        public ActionResult SearchForSeries(string seriesName)
        {
            var model = new List<SeriesSearchResultModel>();

            //Get Results from TvDb and convert them to something we can use.
            foreach (var tvdbSearchResult in _tvDbProvider.SearchSeries(seriesName))
            {
                model.Add(new SeriesSearchResultModel
                              {
                                  TvDbId = tvdbSearchResult.Id,
                                  TvDbName = tvdbSearchResult.SeriesName,
                                  FirstAired = tvdbSearchResult.FirstAired
                              });
            }

            //model.Add(new SeriesSearchResultModel{ TvDbId = 12345, TvDbName = "30 Rock", FirstAired = DateTime.Today });
            //model.Add(new SeriesSearchResultModel { TvDbId = 65432, TvDbName = "The Office (US)", FirstAired = DateTime.Today.AddDays(-100) });

            return PartialView("SeriesSearchResults", model);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        [GridAction]
        public ActionResult _SaveAjaxEditing(string id)
        {
            return RedirectToAction("UnMapped");
        }

        [HttpPost]
        public ActionResult SaveSeasons(List<SeasonEditModel> seasons)
        {
            foreach (var season in seasons)
            {
                if (_episodeProvider.IsIgnored(season.SeriesId, season.SeasonNumber) != !season.Monitored)
                {
                    _episodeProvider.SetSeasonIgnore(season.SeriesId, season.SeasonNumber, !season.Monitored);
                }
            }

            return Content("Saved");
        }

        public ActionResult Details(int seriesId)
        {
            var series = _seriesProvider.GetSeries(seriesId);

            var model = new SeriesModel();

            if (series.AirsDayOfWeek != null)
            {
                model.AirsDayOfWeek = series.AirsDayOfWeek.Value.ToString();
            }
            else
            {
                model.AirsDayOfWeek = "N/A";
            }
            model.Overview = series.Overview;
            model.Seasons = _episodeProvider.GetSeasons(seriesId);
            model.Title = series.Status;
            model.SeriesId = series.SeriesId;

            return View(model);
        }

        public ActionResult SyncEpisodesOnDisk(int seriesId)
        {
            //Syncs the episodes on disk for the specified series
            _jobProvider.QueueJob(typeof(DiskScanJob), seriesId);

            return RedirectToAction("Details", new { seriesId });
        }

        public ActionResult UpdateInfo(int seriesId)
        {
            //Syncs the episodes on disk for the specified series
            _jobProvider.QueueJob(typeof(UpdateInfoJob), seriesId);
            return RedirectToAction("Details", new { seriesId });
        }

        public ActionResult RenameAll()
        {
            _renameProvider.RenameAll();
            return RedirectToAction("Index");
        }

        public ActionResult RenameSeries(int seriesId)
        {
            _renameProvider.RenameSeries(seriesId);
            return RedirectToAction("Details", new { seriesId });
        }

        public ActionResult RenameSeason(int seasonId)
        {
            //Todo: Stay of Series Detail... AJAX?
            _renameProvider.RenameSeason(seasonId);
            return RedirectToAction("Index");
        }

        public ActionResult RenameEpisode(int episodeId)
        {
            //Todo: Stay of Series Detail... AJAX?
            _renameProvider.RenameEpisode(episodeId);
            return RedirectToAction("Index");
        }

        private List<SeriesModel> GetSeriesModels(List<Series> seriesInDb)
        {
            var series = new List<SeriesModel>();

            foreach (var s in seriesInDb)
            {
                series.Add(new SeriesModel
                               {
                                   SeriesId = s.SeriesId,
                                   Title = s.Title,
                                   AirsDayOfWeek = s.AirsDayOfWeek.ToString(),
                                   Monitored = s.Monitored,
                                   Overview = s.Overview,
                                   Path = s.Path,
                                   QualityProfileId = s.QualityProfileId,
                                   QualityProfileName = s.QualityProfile.Name,
                                   SeasonFolder = s.SeasonFolder,
                                   Status = s.Status,
                               });
            }

            return series;
        }

        private string GetSeasonString(int seasonNumber)
        {
            if (seasonNumber == 0)
                return "Specials";

            return String.Format("Season #{0}", seasonNumber);
        }
    }
}