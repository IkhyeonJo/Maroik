﻿"use strict";

$(() => {
    if ($('#fingers10').length !== 0) {
        var table = $('#fingers10').DataTable({
             language: {
                url: langFileUrl
            },
            processing: true,
            serverSide: true,
            orderCellsTop: true,
            autoWidth: true,
            deferRender: true,
            lengthMenu: [[5, 10, 15, 20, -1], [5, 10, 15, 20, "All"]],
            dom: '<"row"<"col-sm-12 col-md-6"B><"col-sm-12 col-md-6 text-right"l>><"row"<"col-sm-12"tr>><"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
            buttons: [
                {
                    text: exportToExcelText,
                    className: 'btn btn-sm btn-dark',
                    action: function (e, dt, node, config) {
                        window.location.href = "/Home/GetExcel";
                    },
                    init: function (api, node, config) {
                        $(node).removeClass('dt-button');
                    }
                },
                {
                    text: createText,
                    className: 'btn btn-sm btn-success',
                    action: function (e, dt, node, config) {
                        $('#createModal').modal('show');
                    },
                    init: function (api, node, config) {
                        $(node).removeClass('dt-button');
                    }
                }
            ],
            ajax: {
                type: "POST",
                url: '/Home/LoadTable/',
                contentType: "application/json; charset=utf-8",
                async: true,
                headers: {
                    "XSRF-TOKEN": document.querySelector('[name="__RequestVerificationToken"]').value
                },
                data: function (data) {
                    let additionalValues = [];
                    additionalValues[0] = "Additional Parameters 1";
                    additionalValues[1] = "Additional Parameters 2";
                    data.AdditionalValues = additionalValues;

                    return JSON.stringify(data);
                }
            },
            columns: [
                {
                    data: "Id",
                    name: "eq",
                    visible: false,
                    searchable: false,
                    orderable: false
                },
                {
                    data: "Name",
                    name: "co"
                },
                {
                    data: "Position",
                    name: "co"
                },
                {
                    data: "Offices",
                    name: "eq"
                },
                {
                    data: "DemoNestedLevelOne.Experience",
                    name: "eq"
                },
                {
                    data: "DemoNestedLevelOne.Extension",
                    name: "eq"
                },
                {
                    data: "DemoNestedLevelOne.DemoNestedLevelTwos.StartDates",
                    render: function (data, type, row) {
                        if (data)
                            return window.moment(data).format("DD/MM/YYYY");
                        else
                            return null;
                    },
                    name: "gt"
                },
                {
                    data: "DemoNestedLevelOne.DemoNestedLevelTwos.Salary",
                    name: "lte"
                },
                {
                    orderable: false,
                    width: 100,
                    data: "DemoNestedLevelOne.DemoNestedLevelTwos.Action",
                    render: function (data, type, row) {
                        return `<div>
                                    <button type="button" class="btn btn-sm btn-info mr-2 btnEdit" data-key="${row.Id}">${editText}</button>
                                    <button type="button" class="btn btn-sm btn-danger btnDelete" data-key="${row.Id}">${deleteText}</button>
                                </div>`;
                    }
                }
            ]
        });

        table.columns().every(function (index) {
            $('#fingers10 thead tr:last th:eq(' + index + ') input')
                .on('keyup',
                    function (e) {
                        if (e.keyCode === 13) {
                            table.column($(this).parent().index() + ':visible').search(this.value).draw();
                        }
                    });
        });

        $(document)
            .off('click', '#btnCreate')
            .on('click', '#btnCreate', function () {
                fetch('/Home/Create/',
                    {
                        method: 'POST',
                        cache: 'no-cache',
                        body: new URLSearchParams(new FormData(document.querySelector('#frmCreate')))
                    })
                    .then((response) => {
                        table.ajax.reload();
                        $('#createModal').modal('hide');
                        document.querySelector('#frmCreate').reset();
                    })
                    .catch((error) => {
                        console.log(error);
                    });
            });

        $(document)
            .off('click', '.btnEdit')
            .on('click', '.btnEdit', function () {
                const id = $(this).attr('data-key');

                fetch(`/Home/Edit/${id}`,
                    {
                        method: 'GET',
                        cache: 'no-cache'
                    })
                    .then((response) => {
                        return response.text();
                    })
                    .then((result) => {
                        $('#editPartial').html(result);
                        $('#editModal').modal('show');
                    })
                    .catch((error) => {
                        console.log(error);
                    });
            });

        $(document)
            .off('click', '#btnUpdate')
            .on('click', '#btnUpdate', function () {
                fetch('/Home/Edit/',
                    {
                        method: 'PUT',
                        cache: 'no-cache',
                        body: new URLSearchParams(new FormData(document.querySelector('#frmEdit')))
                    })
                    .then((response) => {
                        table.ajax.reload();
                        $('#editModal').modal('hide');
                        $('#editPartial').html('');
                    })
                    .catch((error) => {
                        console.log(error);
                    });
            });

        $(document)
            .off('click', '.btnDelete')
            .on('click', '.btnDelete', function () {
                const id = $(this).attr('data-key');

                if (confirm('Are you sure?')) {
                    fetch(`/Home/Delete/${id}`,
                        {
                            method: 'DELETE',
                            cache: 'no-cache'
                        })
                        .then((response) => {
                            table.ajax.reload();
                        })
                        .catch((error) => {
                            console.log(error);
                        });
                }
            });

        $('#btnExternalSearch').click(function () {
            table.column('0:visible').search($('#txtExternalSearch').val()).draw();
        });
    }
});