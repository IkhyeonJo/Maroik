package com.mylaboratory.homepage.controller;

import com.mylaboratory.homepage.entity.Board;
import com.mylaboratory.homepage.repository.BoardRepository;
import com.mylaboratory.homepage.service.BoardService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("Board")
public class BoardController {
    @Autowired
    private BoardService boardService;

    @GetMapping("Main")
    public Board Main() {
        Board board = boardService.GetBoard((long)1);
        return board;
    }
}
