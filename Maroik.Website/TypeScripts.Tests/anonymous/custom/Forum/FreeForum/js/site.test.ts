import { describe, it, expect } from "vitest";
import { loadSite, antiForgery } from "@tests/_common/harness";

// wwwroot/anonymous/custom/Forum/FreeForum/js/site.js — read-only board view.
function fixture(): string {
  return (
    antiForgery +
    `<span id="free_forum_detail_updated">2024-01-02T03:04:05Z</span>
     <table id="tblFreeForum"><tbody><tr>
       <td class="free_forum_board_created">2024-06-07T08:09:10Z</td>
     </tr></tbody></table>
     <span class="free_forum_detail_comment_created">2024-03-04T05:06:07Z</span>
     <select id="searchType"><option value="Title" selected>Title</option></select>
     <input id="btnFreeForumSearchText" value="term" />
     <button id="btnFreeForumSearchBoard"></button><button id="btnFreeForumWrite"></button>
     <a id="btnFreeForumList" data-link="/list"></a>
     <form id="formWriteFreeComment"></form>
     <div id="detailBoardContent"></div><a id="aDetailBoardAttachedFile"></a>`
  );
}

describe("Forum/FreeForum (anonymous)", () => {
  it("rewrites the UTC detail timestamp to the browser locale string", () => {
    const h = loadSite("anonymous", "Forum", "FreeForum", fixture());
    const shown = h.$("#free_forum_detail_updated").text();
    expect(shown).toBe(new Date("2024-01-02T03:04:05Z").toLocaleString());
  });

  it("search navigates to /Forum/FreeForum with the encoded query", () => {
    const h = loadSite("anonymous", "Forum", "FreeForum", fixture());
    h.$("#btnFreeForumSearchBoard").trigger("click");
    expect(h.navigations.at(-1)).toContain("/Forum/FreeForum?searchType=Title");
    expect(h.navigations.at(-1)).toContain("searchText=term");
  });

  it("blocks Enter inside the comment form", () => {
    const h = loadSite("anonymous", "Forum", "FreeForum", fixture());
    const ev: any = h.$.Event("keydown", { key: "Enter" });
    h.$("#formWriteFreeComment").trigger(ev);
    // jQuery stashes the last handler's return value on event.result
    expect(ev.result).toBe(false);
  });
});
