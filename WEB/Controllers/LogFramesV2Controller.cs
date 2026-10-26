using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monic.Web.Code;
using Monic.Web.Models;

namespace Monic.Web.Controllers;

[ApiController, Route("api/v1/logframes"), Route("api/logframes")]
public sealed class LogFramesV2Controller : BaseApiController
{
    private readonly LogFrameTreeService _treeService;

    public LogFramesV2Controller(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        UserManager<User> userManager,
        AppSettings appSettings,
        LogFrameTreeService treeService)
        : base(dbFactory, userManager, appSettings)
    {
        _treeService = treeService;
    }

    /// <summary>
    /// Returns the active LogFrame v2 nodes for a logframe as a flat, ordered list.
    /// The client rebuilds the tree from ParentId/SortOrder. ETag reflects the logframe id.
    /// </summary>
    [HttpGet("{logFrameId:guid}/tree")]
    [ProducesResponseType<LogFrameTreeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LogFrameTreeDto>> GetTree(
        Guid logFrameId, CancellationToken ct)
    {
        var tree = await _treeService.LoadTreeAsync(logFrameId, ct);

        if (tree is null)
            return NotFound();

        Response.Headers.ETag = $"\"{logFrameId:N}\"";
        return Ok(tree);
    }
}
