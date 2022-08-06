package com.mylaboratory.homepage.service;

import com.mylaboratory.homepage.entity.Board;
import com.mylaboratory.homepage.repository.BoardRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestClientException;

@Service
public class BoardService {

    @Autowired
    private BoardRepository boardRepository;

    public Board GetBoard(Long boardId) {
        return boardRepository.findById(boardId).orElseThrow(() ->
                new RestClientException("Not found board", null));
    }

}