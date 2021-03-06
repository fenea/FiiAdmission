﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Helpers;
using Api.ModelView;
using AutoMapper;
using Business.AnnouncementsRepo;
using Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Api.Controllers
{
  [Authorize(AuthenticationSchemes = "Bearer", Policy = "User")]
  [Produces("application/json")]
  [Route("api/Announcement")]
  public class AnnouncementController : Controller
  {
    private readonly IAnnouncementsRepository _announcementsRepository;
    private readonly IMapper _mapper;
    readonly ILogger _logger;

    public AnnouncementController(IAnnouncementsRepository announcementsRepository, IMapper mapper,
      ILoggerFactory loggerFactory)
    {
      _announcementsRepository = announcementsRepository;
      _mapper = mapper;
      _logger = loggerFactory.CreateLogger(nameof(AnnouncementController));
    }

    [HttpGet]
    [NoCache]
    [ProducesResponseType(typeof(List<AnnouncementModel>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> GetAnnouncements()
    {
      try
      {
        var announcementsDtoList = new List<AnnouncementModel>();
        var announcements = await _announcementsRepository.GetAll();
        foreach (var elem in announcements)
        {
          announcementsDtoList.Add(_mapper.Map<AnnouncementModel>(elem));
        }
        return Ok(announcementsDtoList);
      }
      catch (Exception exp)
      {
        _logger.LogError(exp.Message);
        return BadRequest(new ApiResponse {Status = false});
      }
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "Admin")]
    [HttpGet("{id}", Name = "GetAnnouncementsRoute")]
    [NoCache]
    [ProducesResponseType(typeof(AnnouncementModel), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult> Announcements(Guid id)
    {
      try
      {
        var announcement = await _announcementsRepository.GetById(id);
        var announcementModel = _mapper.Map<AnnouncementModel>(announcement);
        return Ok(announcementModel);
      }
      catch (Exception exp)
      {
        _logger.LogError(exp.Message);
        return BadRequest(new ApiResponse {Status = false});
      }
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseObject<AddAnnouncementModel>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> CreateAnnouncement([FromBody] AddAnnouncementModel model)
    {
      if (!ModelState.IsValid)
      {
        return BadRequest(new ApiResponse {Status = false, ModelState = ModelState});
      }
      try
      {
        var announcement = _mapper.Map<Announcement>(model);
        var newAnnouncement = await _announcementsRepository.Add(announcement);
        var newAnnouncementModel = _mapper.Map<AddAnnouncementModel>(announcement);
        if (newAnnouncement == null)
        {
          return BadRequest(new ApiResponse {Status = false});
        }
        return CreatedAtRoute("GetAnnouncementsRoute", new {id = newAnnouncement.Id},
          new ApiResponseObject<AddAnnouncementModel> {Status = true, Object = newAnnouncementModel});
      }
      catch (Exception exp)
      {
        _logger.LogError(exp.Message);
        return BadRequest(new ApiResponse {Status = false});
      }
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "Admin")]
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponseObject<AnnouncementModel>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult> UpdateAnnouncement([FromBody] AnnouncementModel announcementModel)
    {
      if (!ModelState.IsValid)
      {
        return BadRequest(new ApiResponse {Status = false, ModelState = ModelState});
      }

      try
      {
        var announcement = _mapper.Map<Announcement>(announcementModel);
        var status = await _announcementsRepository.Edit(announcement);
        if (!status)
        {
          return BadRequest(new ApiResponse {Status = false});
        }
        return Ok(new ApiResponseObject<AnnouncementModel> {Status = true, Object = announcementModel});
      }
      catch (Exception exp)
      {
        _logger.LogError(exp.Message);
        return BadRequest(new ApiResponse {Status = false});
      }
    }

    [Authorize(AuthenticationSchemes = "Bearer", Policy = "Admin")]
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult> DeleteAnnouncement(Guid id)
    {
      try
      {
        var status = await _announcementsRepository.Delete(id);
        if (!status)
        {
          return BadRequest(new ApiResponse {Status = false});
        }
        return Ok(new ApiResponse {Status = true});
      }
      catch (Exception exp)
      {
        _logger.LogError(exp.Message);
        return BadRequest(new ApiResponse {Status = false});
      }
    }
  }
}