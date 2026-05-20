using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Shuffler.Api;

/// <summary>
/// API endpoints for the Shuffler plugin.
/// </summary>
[ApiController]
[Authorize]
[Route("Shuffler")]
public class ShufflerController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IDtoService _dtoService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShufflerController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    public ShufflerController(ILibraryManager libraryManager, IUserManager userManager, IDtoService dtoService)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _dtoService = dtoService;
    }

    /// <summary>
    /// Gets a shuffled list of video items, excluding permanently skipped ones.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="parentId">Optional library/folder ID to shuffle within.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <returns>Shuffled list of <see cref="BaseItemDto"/>.</returns>
    [HttpGet("Items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<BaseItemDto>> GetShuffledItems(
        [FromQuery, Required] Guid userId,
        [FromQuery] Guid? parentId,
        [FromQuery] int limit = 200)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return NotFound("User not found.");
        }

        var skipped = Plugin.Instance!.Configuration.SkippedItemIds
            .Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToHashSet();

        var query = new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Episode],
            IsVirtualItem = false,
            Recursive = true
        };

        if (parentId.HasValue)
        {
            query.ParentId = parentId.Value;
        }

        var dtoOptions = new DtoOptions(false)
        {
            Fields =
            [
                ItemFields.MediaSources,
                ItemFields.Overview,
                ItemFields.ParentId
            ]
        };

        var items = _libraryManager.GetItemList(query)
            .Where(item => !skipped.Contains(item.Id))
            .OrderBy(_ => Random.Shared.Next())
            .Take(limit)
            .Select(item => _dtoService.GetBaseItemDto(item, dtoOptions, user))
            .ToList();

        return Ok(items);
    }

    /// <summary>
    /// Adds an item to the permanent skip list (bad encoding, unplayable, etc).
    /// </summary>
    /// <param name="itemId">The item ID to skip.</param>
    /// <returns>No content.</returns>
    [HttpPost("Items/{itemId}/Skip")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult SkipItem([FromRoute] Guid itemId)
    {
        var config = Plugin.Instance!.Configuration;
        var idStr = itemId.ToString();

        if (!config.SkippedItemIds.Contains(idStr))
        {
            config.SkippedItemIds.Add(idStr);
            Plugin.Instance.SaveConfiguration();
        }

        return NoContent();
    }

    /// <summary>
    /// Removes an item from the skip list.
    /// </summary>
    /// <param name="itemId">The item ID to un-skip.</param>
    /// <returns>No content.</returns>
    [HttpDelete("Items/{itemId}/Skip")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UnSkipItem([FromRoute] Guid itemId)
    {
        var config = Plugin.Instance!.Configuration;
        config.SkippedItemIds.Remove(itemId.ToString());
        Plugin.Instance.SaveConfiguration();
        return NoContent();
    }

    /// <summary>
    /// Gets all skipped item IDs.
    /// </summary>
    /// <returns>List of skipped item ID strings.</returns>
    [HttpGet("SkipList")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<string>> GetSkipList()
    {
        return Ok(Plugin.Instance!.Configuration.SkippedItemIds);
    }
}
