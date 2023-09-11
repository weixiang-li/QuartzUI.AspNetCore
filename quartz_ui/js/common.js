$.namespace("quartz.common");

quartz.common = (function () {
  return {
    ajaxRequestForReset: function () {
      if (!confirm("重置所有配置？")) {
        return;
      }

      $.ajax({
        type: "GET",
        url: "api/ResetAll",
        success: function (data) {
          if ("success" == data) {
            alert("重置成功");
          }
        },
        dataType: "text",
      });
    },

    ajaxRequestForResetLog: function (key = '') {
      if (!confirm("重置所有日志？")) {
        return;
      }

      $.ajax({
        type: "GET",
        url: "api/ResetLog?key=" + key,
        success: function (data) {
          if ("success" == data) {
            alert("重置成功");
          }
        },
        dataType: "text",
      });
    },

    stripes: function () {
      $("#dataTable tbody tr").each(function () {
        $(this).removeClass("striped");
      });
      $("#dataTable tbody tr:even").each(function () {
        $(this).addClass("striped");
      });
    },

    getUrlVar: function (name) {
      var vars = {};
      var parts = window.location.href.replace(
        /[?&]+([^=&]+)=([^&]*)/gi,
        function (m, key, value) {
          vars[key] = value;
        }
      );
      return vars[name] == undefined ? '' : vars[name];
    },
  };
})();
